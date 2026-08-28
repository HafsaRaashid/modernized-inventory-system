import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // Local-dev-only proxy: keeps the SPA and the API same-origin from the
    // browser's point of view (SQ-003: self-hosted, on-prem, single-tenant
    // deployment), so no CORS policy is needed on either side — see
    // .specclaw/bootstrap/bootstrap-plan.md "Boundaries" for the full
    // rationale. In production this API instead serves the SPA's own build
    // output, which is same-origin by construction.
    proxy: {
      "/api": {
        target: "http://localhost:5080",
        changeOrigin: true,
      },
    },
  },
  test: {
    environment: "jsdom",
    setupFiles: "./tests/setupTests.ts",
    globals: true,
  },
});
