# NOVORA-LINK 1.3.1 — Puente de actualización

> **Español (principal) · English below**

## Español

NOVORA-LINK 1.3.1 es una versión de mantenimiento creada específicamente para corregir el canal de actualizaciones utilizado por NOVORA-LINK 1.3.0.

### Motivo

Las instalaciones 1.3.0 fueron compiladas cuando el actualizador consultaba `aroonvaldes-star/NOVORA-PROYECT`. El repositorio principal del proyecto actualmente es `aroonvaldes-star/NOVORA-LINK`.

La 1.3.1 funciona como puente:

```text
1.3.0 → NOVORA-PROYECT → 1.3.1 → NOVORA-LINK → 1.4+
```

### Cambios

- Versión actualizada a `1.3.1`.
- `UpdateService` redirigido a `aroonvaldes-star/NOVORA-LINK`.
- Endpoint de releases actualizado a `https://api.github.com/repos/aroonvaldes-star/NOVORA-LINK/releases/latest`.
- Fallback interno de versión actualizado a `1.3.1`.
- User-Agent del actualizador actualizado a `NOVORA/1.3.1`.
- Títulos visibles actualizados a 1.3.1.
- Instalador generado como `NOVORA-Setup-1.3.1.exe`.
- Se conserva el mismo `AppId` del instalador de 1.3 para permitir actualización sobre la instalación existente.

### Sin cambios funcionales de 1.4

Esta release no incorpora el desarrollo actual de LinkEngine, VisionEngine ni otras funciones nuevas de 1.4. Mantiene la base funcional de la release pública 1.3 basada en ADB, scrcpy y Gnirehtet.

### Después de instalar

La 1.3.1 ya no consulta este repositorio para actualizaciones normales. Las siguientes versiones se detectarán desde:

`https://github.com/aroonvaldes-star/NOVORA-LINK/releases`

---

## English

NOVORA-LINK 1.3.1 is a maintenance bridge release created specifically to repair the update channel used by NOVORA-LINK 1.3.0.

It changes the updater from the legacy `aroonvaldes-star/NOVORA-PROYECT` repository to the official `aroonvaldes-star/NOVORA-LINK` repository while preserving the functional 1.3 codebase.

No current LinkEngine, VisionEngine or other 1.4 development changes are included.

Migration path:

`1.3.0 → NOVORA-PROYECT → 1.3.1 → NOVORA-LINK → 1.4+`
