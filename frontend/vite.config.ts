import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    host: "127.0.0.1",
    port: 5176,
    strictPort: true,
    proxy: {
      "/realtime": {target:"http://127.0.0.1:5092",ws:true},
      "/api": "http://127.0.0.1:5092",
      "/health": "http://127.0.0.1:5092",
    },
  },
});
