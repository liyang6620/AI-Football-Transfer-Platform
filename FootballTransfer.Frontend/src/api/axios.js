import axios from "axios";

const API_BASE =
    import.meta.env.VITE_API_BASE_URL || "https://football-transfer-api.onrender.com";

const api = axios.create({
    baseURL: `${API_BASE}/api`,
    timeout: 30000,
});

export const API_STATE_EVENT = "football-api-state";

function publishApiState(state) {
    if (typeof window !== "undefined") {
        window.dispatchEvent(new CustomEvent(API_STATE_EVENT, { detail: state }));
    }
}

function shouldRetry(error) {
    const status = error.response?.status;
    return !error.response || status >= 500;
}

export async function getWithRecovery(path, config = {}) {
    let lastError;

    for (let attempt = 0; attempt < 2; attempt += 1) {
        try {
            const response = await api.get(path, config);
            publishApiState("ready");
            return response;
        } catch (error) {
            lastError = error;
            if (attempt === 0 && shouldRetry(error)) {
                publishApiState("waking");
                await new Promise((resolve) => setTimeout(resolve, 2500));
                continue;
            }
            publishApiState("unavailable");
            throw error;
        }
    }

    publishApiState("unavailable");
    throw lastError;
}

export default api;
