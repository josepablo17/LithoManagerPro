# LithoManager.Client

Frontend de LithoManagerPro construido con React, JavaScript y Vite.

## Stack

- React + JavaScript
- Vite
- CSS Modules + CSS tradicional
- React Router
- Lucide Icons
- ESLint
- Vitest + React Testing Library

## Scripts

```bash
npm install
npm run dev
npm run lint
npm test
npm run build
```

## API

Durante desarrollo, Vite proxy redirige `/api` hacia:

```text
https://localhost:7201
```

Para apuntar a otra URL, define:

```bash
VITE_API_BASE_URL=https://localhost:7201
```

No se deben guardar secretos, cadenas de conexión ni claves privadas en variables accesibles al frontend.
