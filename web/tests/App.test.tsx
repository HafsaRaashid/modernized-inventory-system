import { render, screen } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import App from "../src/App";

/**
 * One trivial test proving the frontend test runner (test-frontend pillar)
 * executes end to end against this foundation. It renders only the app
 * shell — no backlog capability.
 */
describe("App shell", () => {
  it("renders without crashing", () => {
    render(
      <BrowserRouter>
        <App />
      </BrowserRouter>,
    );

    expect(screen.getByText("Inventory Tracking System")).toBeInTheDocument();
  });
});
