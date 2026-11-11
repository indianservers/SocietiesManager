import React, { Suspense, lazy } from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import "./index.css";
const App = lazy(() => import("./App"));
ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <QueryClientProvider
      client={
        new QueryClient({
          defaultOptions: { queries: { staleTime: 30000, retry: false } },
        })
      }
    >
      <BrowserRouter>
        <Suspense fallback={<p>Loading workspace…</p>}>
          <App />
        </Suspense>
      </BrowserRouter>
    </QueryClientProvider>
  </React.StrictMode>,
);
