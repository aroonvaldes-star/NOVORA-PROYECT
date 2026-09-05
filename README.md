# NOVORA-PROYECT — Puente legado de actualización

> **Español (principal) · English below**

## Español

Este repositorio **no es el repositorio principal de desarrollo de NOVORA-LINK**.

`NOVORA-PROYECT` existe únicamente como **puente de actualización para instalaciones de NOVORA-LINK 1.3.0** que fueron compiladas cuando el actualizador todavía consultaba este nombre de repositorio.

Repositorio oficial actual:

**https://github.com/aroonvaldes-star/NOVORA-LINK**

### Flujo de migración

```text
NOVORA-LINK 1.3.0
        │
        │ consulta NOVORA-PROYECT/releases/latest
        ▼
NOVORA-PROYECT
        │
        ▼
NOVORA-LINK 1.3.1 — Update Bridge
        │
        │ cambia el canal de actualizaciones
        ▼
aroonvaldes-star/NOVORA-LINK
        │
        ▼
NOVORA-LINK 1.4 y posteriores
```

### Qué contiene 1.3.1

La versión 1.3.1 conserva la base funcional publicada de NOVORA-LINK 1.3 y modifica únicamente lo necesario para la transición:

- versión de aplicación `1.3.1`;
- canal de actualizaciones apuntando a `aroonvaldes-star/NOVORA-LINK`;
- API `https://api.github.com/repos/aroonvaldes-star/NOVORA-LINK/releases/latest`;
- identificadores visibles de versión actualizados a 1.3.1;
- instalador `NOVORA-Setup-1.3.1.exe`;
- compilación, pruebas, SHA-256 y publicación automatizadas mediante GitHub Actions.

**No se incorpora aquí el desarrollo actual de LinkEngine, VisionEngine ni otros cambios de la línea 1.4.**

### Fuente fijada

El puente se construye automáticamente tomando como base exacta el commit que fue objetivo de la release pública `v1.3`:

`305338e2477b2194673479a69bf90152fc89744a`

El workflow aplica después `scripts/Apply-Bridge-1.3.1.ps1`, ejecuta pruebas, publica la aplicación self-contained para Windows x64 y genera el instalador.

### Estado

Este repositorio debe mantenerse disponible mientras puedan existir instalaciones 1.3.0 que necesiten migrar.

Una vez instalada la 1.3.1, las futuras actualizaciones dejan de depender de este repositorio y se consultan desde `NOVORA-LINK`.

---

## English

This repository is **not the main NOVORA-LINK development repository**.

`NOVORA-PROYECT` exists only as a **legacy update bridge for NOVORA-LINK 1.3.0 installations** compiled while the updater still referenced this repository name.

Current official repository:

**https://github.com/aroonvaldes-star/NOVORA-LINK**

The bridge is built from the exact commit targeted by the public `v1.3` release (`305338e2477b2194673479a69bf90152fc89744a`). It changes the application version to `1.3.1`, redirects the updater permanently to `aroonvaldes-star/NOVORA-LINK`, runs tests, builds the Windows x64 installer and publishes the stable `v1.3.1` bridge release.

No current LinkEngine, VisionEngine or NOVORA-LINK 1.4 development code is included in this legacy bridge.

After 1.3.1 is installed, future updates are retrieved from the official `NOVORA-LINK` repository.

---

**Copyright © 2026 Aaron Yair Galarza Valdes.**
