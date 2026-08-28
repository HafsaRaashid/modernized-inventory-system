/**
 * The catch-all error route (frontend-routing pillar). Not a backlog
 * capability — every SPA needs a defined behavior for an unmatched path.
 */
export function NotFound() {
  return (
    <div className="not-found">
      <h1>404</h1>
      <p>This page does not exist.</p>
    </div>
  );
}
