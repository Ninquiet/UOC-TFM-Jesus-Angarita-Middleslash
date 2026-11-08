using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Aberracion {
// Opt-in integration check for the actual Windows player; no effect in ordinary play.
public sealed class PrototypeSmokeTest : MonoBehaviour {
    IEnumerator Start() {
        if (Array.IndexOf(Environment.GetCommandLineArgs(),"-prototypeSmokeTest") < 0) yield break;
        yield return RunScenario(true);
    }
    public IEnumerator RunScenario(bool quit = false) {
        yield return null;
        var actor=FindFirstObjectByType<PrototypePlayer>();
        actor.AcceptInput=false;
        var target=FindEnemies()[0];
        actor.transform.position=target.transform.position+Vector3.back*2; actor.transform.rotation=Quaternion.identity; Physics.SyncTransforms();
        float initialHealth=target.Health;
        var actions = actor.GetComponent<CharacterActionStateMachine>();
        string result;
        try
        {
            actions.Tick(new CharacterActionInput(Vector3.zero, Vector3.forward, true, false), 0);
            Check(actions.Phase == CharacterActionPhase.Windup && actions.IsDrawAttack, "draw attack starts in windup");
            Check(actions.WindupDuration < actions.CurrentMove.windup, "draw attack has shorter preparation");
            Vector3 start = actor.transform.position;
            actions.Tick(new CharacterActionInput(Vector3.right, Vector3.right, false, false), .1f);
            Check(Vector2.Distance(new Vector2(start.x, start.z), new Vector2(actor.transform.position.x, actor.transform.position.z)) < .001f,
                "windup blocks horizontal movement");
            Check(Vector3.Angle(Vector3.forward, actor.transform.forward) <= actions.maxAimCorrection + .01f,
                "aim correction is limited");
            actions.Tick(new CharacterActionInput(Vector3.right, Vector3.left, false, false), actions.WindupDuration);
            Vector3 lockedDirection = actor.transform.forward;
            Check(actions.Phase == CharacterActionPhase.Strike && target.Health == initialHealth, "windup ends before impact");
            actions.Tick(new CharacterActionInput(Vector3.right, Vector3.left, false, false), actions.CurrentMove.strike);
            Vector3 traveled = actor.transform.position - start;
            traveled.y = 0;
            Check(actions.Phase == CharacterActionPhase.AttackLink && target.Health < initialHealth, "strike resolves impact");
            Check(traveled.magnitude <= actions.CurrentMove.advance + .01f && Vector3.Angle(lockedDirection, actor.transform.forward) < .01f,
                "strike has bounded movement and locked aim");
            actions.Tick(default, actions.LinkDelay);
            Check(actions.Phase == CharacterActionPhase.Recovery, "single swing enters recovery");
            Vector3 recoveryStart = actor.transform.position;
            actions.Tick(new CharacterActionInput(Vector3.right, Vector3.right, false, true), actions.CurrentMove.recovery * .5f);
            Vector3 recoveryMove = actor.transform.position - recoveryStart;
            recoveryMove.y = 0;
            Check(actions.Phase == CharacterActionPhase.Recovery && recoveryMove.sqrMagnitude < .000001f,
                "recovery blocks sheathing and movement");
            actions.Tick(default, actions.CurrentMove.recovery);
            Check(actions.Phase == CharacterActionPhase.Drawn, "recovery ends with sword drawn");
            actions.Tick(new CharacterActionInput(Vector3.zero, Vector3.zero, false, true), 0);
            var vitals = actor.GetComponent<PlayerCombatVitals>();
            vitals.ReceivePunch(10, actor.transform.position + Vector3.forward);
            actions.Tick(default, actions.fallDuration);
            actions.Tick(default, actions.getUpDuration);
            Check(actions.Phase == CharacterActionPhase.Drawn, "interrupted sheathing restores the drawn stance");
            vitals.actor.RestoreHealth();
            actions.Tick(new CharacterActionInput(Vector3.zero, Vector3.zero, false, true), 0);
            actions.Tick(default, actions.sheathingDuration);
            Check(actions.Phase == CharacterActionPhase.Sheathed, "sheathing restores the initial state");
            CheckKnockdown(actor, actions);
            CheckCombo(actor, actions);
            result = RunChecks();
        }
        catch (Exception error)
        {
            result = "FAILED: " + error;
        }
        string path=Path.Combine(Application.persistentDataPath,"smoke-result.txt");
        File.WriteAllText(path,result); Debug.Log(result);
        if (quit) Application.Quit(result.StartsWith("PASSED") ? 0 : 1);
        actor.GetComponent<FocusState>().ResetState(); foreach(var enemy in FindEnemies()) enemy.RestoreHealth(); actor.AcceptInput=true;
    }
    void CheckKnockdown(PrototypePlayer player, CharacterActionStateMachine actions)
    {
        player.transform.SetPositionAndRotation(new Vector3(0, .05f, -3), Quaternion.identity);
        Physics.SyncTransforms();
        var vitals = player.GetComponent<PlayerCombatVitals>();
        var command = new CharacterActionInput(Vector3.right, Vector3.right, true, false);
        Vector3 start = player.transform.position;
        float initialHealth = vitals.actor.Health;

        vitals.ReceivePunch(10, start + Vector3.forward);
        Check(actions.Phase == CharacterActionPhase.Knockdown && !actions.RecoverWithWeaponDrawn,
            "punch starts knockdown with the sword sheathed");
        actions.Tick(command, actions.fallDuration);
        Vector3 landed = player.transform.position;
        Check(actions.Phase == CharacterActionPhase.GetUp && landed.z < start.z - .3f,
            "knockdown pushes away from the attacker and enters get-up");
        vitals.ReceivePunch(10, start + Vector3.forward);
        Check(Mathf.Approximately(vitals.actor.Health, initialHealth - 10), "downed character cannot be hit again");

        actions.Tick(command, actions.getUpDuration * .5f);
        Check(!vitals.IsInvulnerable, "protection ends halfway through get-up");
        actions.Tick(command, actions.getUpDuration * .5f - .01f);
        Vector3 displacement = player.transform.position - landed;
        displacement.y = 0;
        Check(actions.Phase == CharacterActionPhase.GetUp && displacement.sqrMagnitude < .000001f,
            "get-up blocks movement and attacks for 1.5 seconds");
        actions.Tick(command, .02f);
        Check(actions.Phase == CharacterActionPhase.Sheathed, "get-up returns to the prior stance");
        vitals.actor.RestoreHealth();
    }

    void CheckCombo(PrototypePlayer player, CharacterActionStateMachine actions)
    {
        var enemies = FindEnemies();
        var focus = player.GetComponent<FocusState>();
        var primary = enemies[0];
        var secondary = enemies[1];
        Vector3 originalPrimary = primary.transform.position;
        Vector3 originalSecondary = secondary.transform.position;
        var attack = new CharacterActionInput(Vector3.zero, Vector3.forward, true, false);

        try
        {
            focus.ResetState();
            player.transform.SetPositionAndRotation(new Vector3(0, .05f, 0), Quaternion.identity);
            primary.transform.position = new Vector3(0, 1, 2.25f);
            secondary.transform.position = new Vector3(1.3f, 1, 1.35f);
            Physics.SyncTransforms();
            float primaryHealth = primary.Health;
            float secondaryHealth = secondary.Health;

            actions.Tick(attack, 0);
            actions.Tick(default, actions.WindupDuration);
            actions.Tick(attack, actions.CurrentMove.strike);
            Check(primary.Health < primaryHealth && secondary.Health == secondaryHealth, "opening cut hits one target");
            Check(focus.Target == primary, "opening cut sets the focus target");
            actions.Tick(default, actions.LinkDelay);
            Check(actions.MoveIndex == 1 && actions.Phase == CharacterActionPhase.Windup, "opening cut links into sweep");

            primaryHealth = primary.Health;
            float focusBeforeSweep = focus.Progress;
            actions.Tick(default, actions.WindupDuration);
            actions.Tick(default, actions.CurrentMove.strike);
            Check(primary.Health < primaryHealth && secondary.Health < secondaryHealth, "sweep damages both targets");
            Check(focus.Target == primary && Mathf.Approximately(focus.Progress, focusBeforeSweep + focus.progressPerHit),
                "sweep counts as one focus hit on the frontal target");
            actions.Tick(default, actions.LinkDelay);
            Check(actions.Phase == CharacterActionPhase.Recovery, "unbuffered sweep enters recovery");
            actions.Tick(attack, actions.CurrentMove.recovery * .9f);
            Check(actions.MoveIndex == 2 && actions.Phase == CharacterActionPhase.Windup,
                "late recovery input links sweep into finisher");

            actions.Tick(default, actions.WindupDuration);
            actions.Tick(attack, actions.CurrentMove.strike);
            actions.Tick(attack, actions.CurrentMove.recovery * .8f);
            Check(actions.Phase == CharacterActionPhase.Recovery && actions.MoveIndex == 2,
                "finisher enters locked recovery");
            actions.Tick(attack, actions.CurrentMove.recovery);
            Check(actions.Phase == CharacterActionPhase.Drawn, "finisher recovery ignores queued attacks");
        }
        finally
        {
            primary.transform.position = originalPrimary;
            secondary.transform.position = originalSecondary;
            primary.RestoreHealth();
            secondary.RestoreHealth();
            focus.ResetState();
            Physics.SyncTransforms();
        }
    }

    static DamageableActor[] FindEnemies()
    {
        return Array.FindAll(FindObjectsByType<DamageableActor>(FindObjectsSortMode.None),
            actor => actor.team == CombatTeam.Enemy);
    }

    int assertions;
    void Check(bool value,string name) { assertions++; if(!value) throw new Exception(name); }
    static string ReadCsv(string path) { using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite)) using(var reader=new StreamReader(stream)) return reader.ReadToEnd(); }
    string RunChecks() {
        var player=FindFirstObjectByType<PrototypePlayer>(); player.AcceptInput=false;
        var focus=player.GetComponent<FocusState>(); var enemies=FindEnemies();
        var a=enemies[0]; var b=enemies[1];
        Check(enemies.Length==3,"three enemies");
        var motor = player.GetComponent<CharacterMotor>();
        var attack = player.GetComponent<MeleeAttack>();
        Vector3 old=player.transform.position; motor.Move(player.viewCamera.transform.right,.2f);
        Check(Vector3.Distance(old,player.transform.position)>.5f,"movement");
        Check(player.viewCamera.orthographic && player.viewCamera.transform.forward.y<-.4f,"isometric camera");
        player.transform.position=a.transform.position+Vector3.back*2; player.transform.rotation=Quaternion.identity; Physics.SyncTransforms();
        float hp=a.Health; Check(attack.ResolveImpact()==a && a.Health<hp,"sword detection and damage");
        focus.ResetState(); focus.Hit(a,1); float p=focus.Progress; focus.Hit(a,1); Check(focus.Progress>p,"same-target accumulation");
        focus.Hit(b,1); Check(focus.Target==b && Mathf.Approximately(focus.Progress,focus.progressPerHit),"target switch reset");
        focus.Tick(focus.decayDelay+1); Check(focus.Progress==0,"decay");
        focus.ResetState(); for(int i=0;i<10;i++) focus.Hit(a,1); Check(!focus.IsHyper,"probability gate");
        focus.Hit(a,0); Check(focus.IsHyper,"hyperfocus entry"); Check(focus.DamageFor(a)>1 && focus.DamageFor(b)==1,"target-specific bonus");
        hp=a.Health; attack.ResolveImpact(); Check(hp-a.Health>attack.baseDamage,"increased damage through combat");
        focus.Tick(focus.decayDelay+.01f); Check(focus.IsExiting,"inactivity starts exit"); focus.Tick(focus.exitDuration+1); Check(!focus.IsHyper && focus.Progress==0,"gradual exit completes");
        focus.ResetState(); for(int i=0;i<10 && !focus.IsHyper;i++) focus.Hit(a,0);
        focus.Hit(b, 0);
        Check(focus.IsExiting && focus.Target == a, "outgoing hyperfocus keeps its target while fading");
        Check(focus.DamageFor(b) == 1, "new target does not inherit outgoing bonus");
        float fadeStep = focus.exitDuration / (focus.minimumHits + 1);
        for (int i = 1; i < focus.minimumHits; i++)
        {
            focus.Tick(fadeStep);
            focus.Hit(b, 0);
        }
        Check(focus.Target == a && focus.HyperStrength < 1, "outgoing strength fades during new hits");
        focus.Tick(fadeStep * 2 + .01f);
        Check(focus.IsHyper && !focus.IsExiting && focus.Target == b, "hits during fade establish new hyperfocus");
        focus.ResetState();
        a.RestoreHealth();
        b.RestoreHealth();
        Check(a.Health==a.maxHealth && b.Health==b.maxHealth,"enemy reset");
        string csv=ReadCsv(focus.telemetry.FilePath);
        foreach(string kind in new[]{"ataque_inicio","ataque_fin","impacto","objetivo_cambio","hiperfoco_inicio","hiperfoco_fin"}) Check(csv.Contains(kind),"telemetry "+kind);
        focus.telemetry.recording=false; focus.telemetry.Record("should_not_record");
        Check(!ReadCsv(focus.telemetry.FilePath).Contains("should_not_record"),"telemetry toggle");
        return "PASSED: " + assertions + " integration assertions; movement, camera, sword, damage, Focus accumulation/switch/decay, probability gate, hyperfocus bonus/exit, reset, CSV and toggle. Automated API-driven checks; manual input and visual/audio recognition not verified.";
    }
}}



