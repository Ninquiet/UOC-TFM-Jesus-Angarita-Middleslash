# Validación del prototipo PEC 2 — 0.1.0-pec2

Fecha local: 6 de noviembre de 2026. Implementación funcional con validación automática y revisión visual de cámara; pendiente comprobación manual completa de controles y señales.

Los resultados de build y Play Mode que siguen corresponden a la entrega inicial. La revisión posterior separa los componentes y añade cuatro paneles de Focus y números de daño. Se comprobó que los scripts compilan y que la escena guardada conserva las referencias necesarias. La prueba jugable de esta revisión queda pendiente.

## Evidencia

- Unity 6000.3.5f2; proyecto URP existente, Input System existente. Sin nuevas dependencias de juego.
- Escena `Assets/Scenes/PrototypePEC2.unity`, incluida como escena inicial de build. SampleScene original conservada.
- Compilación Windows x86_64 completada: **Succeeded, 0 errores, 0 advertencias**, 101877781 bytes de contenido informado por Unity.
- Ejecutable probado con `-batchmode -nographics -prototypeSmokeTest`: código de salida **0**, **23 comprobaciones aprobadas**.
- El mismo escenario de integración pasó en Play Mode.
- Segunda ejecución Windows con gráficos habilitados: código de salida 0 y 23 comprobaciones aprobadas; inicialización del renderizador Direct3D confirmada en el registro.
- Captura de la cámara a 1280×720 revisada: arena, perspectiva isométrica, protagonista, espada y dos objetivos visibles; materiales renderizados.
- Consulta de consola tras la build: sin errores ni advertencias en la respuesta.
- Todos los recursos nuevos de Assets cuentan con `.meta`.

## Cobertura automática

| Criterio | Resultado |
| --- | --- |
| Movimiento mediante controlador | Aprobado por llamada programática |
| Cámara ortográfica inclinada | Aprobado; captura revisada |
| Ciclo temporal del ataque y daño | Aprobado |
| Detección del enemigo frontal | Aprobado |
| Aumento de Focus sobre el mismo enemigo | Aprobado |
| Reinicio prehiperfoco al cambiar de objetivo | Aprobado |
| Disipación por inactividad | Aprobado |
| Condición y probabilidad de entrada | Aprobado con muestras controladas |
| Daño adicional solo sobre el objetivo de hiperfoco | Aprobado |
| Salida gradual y cambio durante hiperfoco | Aprobado |
| Reinicio de enemigos | Aprobado |
| CSV con los seis eventos previstos y desactivación | Aprobado |
| Arranque del ejecutable Windows | Aprobado en comprobación sin gráficos y con Direct3D |
| Teclado y ratón físicos, HUD y audio en ventana | Pendiente de comprobación manual |
| Legibilidad, comodidad y significado metafórico | Pendiente de evaluación humana |

La primera build exploratoria se sustituyó: contenía incidencias de importación y la primera comprobación detectó un fallo de lectura concurrente del CSV en el código de prueba. La build final y la comprobación corregida son las que sustentan los resultados anteriores.

## Comprobación manual recomendada

1. Abrir `Builds/PEC2-Windows/AberracionPEC2.exe` y verificar que el HUD se lea.
2. Moverse con WASD/flechas, acercarse a un enemigo y atacar apuntando con el ratón.
3. Repetir impactos, observar cambios de objetivo y la entrada incierta en hiperfoco.
4. Dejar de atacar para comprobar visualmente la salida gradual.
5. Probar R, F2, F3 y F4; revisar el CSV de la sesión.
6. Cerrar con Escape.

No se ha realizado evaluación con participantes. Los valores son provisionales. No se ha implementado Calma, Chaos, Time, narrativa ni arte final.


