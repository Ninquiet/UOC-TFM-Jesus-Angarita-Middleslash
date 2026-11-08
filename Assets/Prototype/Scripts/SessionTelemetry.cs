using System;
using System.Globalization;
using System.IO;
using UnityEngine;
namespace Aberracion {
public sealed class SessionTelemetry : MonoBehaviour {
    public bool recording = true;
    public MeleeAttack attack;
    StreamWriter writer;
    public string FilePath { get; private set; }
    public void Record(string kind, string target = "", string detail = "") {
        if (!recording) { Close(); return; }
        try {
            if (writer == null) {
                string dir = Path.Combine(Application.persistentDataPath, "Telemetry");
                Directory.CreateDirectory(dir);
                FilePath = Path.Combine(dir, "session_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + ".csv");
                writer = new StreamWriter(FilePath); writer.AutoFlush = true;
                writer.WriteLine("seconds,event,target,detail");
            }
            writer.WriteLine(Time.realtimeSinceStartup.ToString("F3", CultureInfo.InvariantCulture) + "," + Cell(kind) + "," + Cell(target) + "," + Cell(detail));
        } catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) {
            Debug.LogWarning("Telemetry disabled: " + e.Message); recording = false; Close();
        }
    }
    static string Cell(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
    void Close() { writer?.Dispose(); writer = null; }
    void OnEnable()
    {
        if (!attack) return;
        attack.Started += RecordAttackStart;
        attack.Finished += RecordAttackFinish;
        attack.Hit += RecordHit;
    }

    void OnDisable()
    {
        if (attack)
        {
            attack.Started -= RecordAttackStart;
            attack.Finished -= RecordAttackFinish;
            attack.Hit -= RecordHit;
        }
        Close();
    }

    void RecordAttackStart() => Record("ataque_inicio");
    void RecordAttackFinish() => Record("ataque_fin");
    void RecordHit(DamageableActor target, float damage) =>
        Record("impacto", target.actorId, damage.ToString(CultureInfo.InvariantCulture));
    void OnApplicationQuit() => Close();
}}

