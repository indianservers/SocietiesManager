import React, { Suspense, lazy } from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import "./index.css";
import "./design-tokens.css";
import "./app-ui.css";
const App = lazy(() => import("./App"));
ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <QueryClientProvider
      client={
        new QueryClient({
          defaultOptions: {
            queries: {
              staleTime: 30000,
              retry: false,
              // Returning to a tab must not restart page queries or editors.
              refetchOnWindowFocus: false,
              refetchOnReconnect: false,
            },
          },
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
