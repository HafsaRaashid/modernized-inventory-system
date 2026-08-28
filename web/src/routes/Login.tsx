import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import "./Login.css";
import { login } from "../api/auth";
import { useAuth } from "../auth/AuthContext";

const LOGIN_FAILURE_MESSAGE = "Hatalı giriş yaptınız. Lütfen tekrar giriniz!!!";

/**
 * Sign In screen (SCR-001, TK-001). Reproduces the legacy layout: two
 * stacked icon+field rows (Username, then masked Password) followed by
 * one wide primary action button.
 *
 * Wired to POST /api/auth/login (BL-001 T12): on success, stores the
 * session via AuthContext and navigates to "/"; on failure, shows the
 * legacy rejection message and resets both fields (AC-2/AC-3). The
 * onBlur indicators are purely cosmetic (FR-5/AC-4) — they never gate
 * submission, matching the legacy screen's lack of a redundant
 * pre-check (GM-013).
 */
export function Login() {
  const navigate = useNavigate();
  const { login: authLogin } = useAuth();

  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [usernameEmpty, setUsernameEmpty] = useState(false);
  const [passwordEmpty, setPasswordEmpty] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    try {
      const result = await login(username, password);
      authLogin(result.token, result.username);
      navigate("/");
    } catch {
      setError(LOGIN_FAILURE_MESSAGE);
      setUsername("");
      setPassword("");
    }
  }

  return (
    <div className="login">
      <form id="login-form" className="login__form" onSubmit={handleSubmit}>
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
            value={username}
            onChange={(event) => {
              setUsername(event.target.value);
              if (event.target.value !== "") {
                setUsernameEmpty(false);
              }
            }}
            onBlur={(event) => setUsernameEmpty(event.target.value === "")}
          />
        </div>
        {usernameEmpty && (
          <span className="login__field-hint" style={{ color: "red", fontSize: "0.75rem" }}>
            *
          </span>
        )}
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
            value={password}
            onChange={(event) => {
              setPassword(event.target.value);
              if (event.target.value !== "") {
                setPasswordEmpty(false);
              }
            }}
            onBlur={(event) => setPasswordEmpty(event.target.value === "")}
          />
        </div>
        {passwordEmpty && (
          <span className="login__field-hint" style={{ color: "red", fontSize: "0.75rem" }}>
            *
          </span>
        )}
        {error && (
          <span className="login__error" role="alert" style={{ color: "red", fontSize: "0.85rem" }}>
            {error}
          </span>
        )}
        <button type="submit" className="login__button">
          GİRİŞ
        </button>
      </form>
    </div>
  );
}
