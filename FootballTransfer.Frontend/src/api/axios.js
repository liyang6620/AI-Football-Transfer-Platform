import axios from "axios";

const API_BASE =
    import.meta.env.VITE_API_BASE_URL || "https://football-transfer-api.onrender.com";

const api = axios.create({
    baseURL: `${API_BASE}/api`,
    timeout: 15000,
});

export default api;