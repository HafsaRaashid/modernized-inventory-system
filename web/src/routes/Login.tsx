import "./Login.css";

/**
 * Sign In screen (SCR-001, TK-001). Reproduces the legacy layout: two
 * stacked icon+field rows (Username, then masked Password) followed by
 * one wide primary action button.
 *
 * Unwired for now — no submit handler, no navigation, no API call. A
 * later task wires the credential check (T15) onto this static markup.
 */
export function Login() {
  return (
    <div className="login">
      <form id="login-form" className="login__form">
        <div className="login__field-row">
          <svg
            className="login__icon"
            viewBox="0 0 24 24"
            fill="currentColor"
            aria-hidden="true"
          >
            <path d="M12 12a5 5 0 1 0-5-5 5 5 0 0 0 5 5Zm0 2c-4.42 0-8 2.24-8 5v1h16v-1c0-2.76-3.58-5-8-5Z" />
          </svg>
          <input
            id="username"
            name="username"
            type="text"
            className="login__input"
            placeholder="Kullanıcı Adı"
          />
        </div>
        <div className="login__field-row">
          <svg
            className="login__icon"
            viewBox="0 0 24 24"
            fill="currentColor"
            aria-hidden="true"
          >
            <path d="M17 9V7a5 5 0 0 0-10 0v2a3 3 0 0 0-3 3v7a3 3 0 0 0 3 3h10a3 3 0 0 0 3-3v-7a3 3 0 0 0-3-3ZM9 7a3 3 0 0 1 6 0v2H9Z" />
          </svg>
          <input
            id="password"
            name="password"
            type="password"
            className="login__input"
            placeholder="Şifre"
          />
        </div>
        <button type="submit" className="login__button">
          GİRİŞ
        </button>
      </form>
    </div>
  );
}
