const BASE =
    import.meta.env.VITE_API_BASE_URL || "https://football-transfer-api.onrender.com";

async function request(path) {
    const res = await fetch(`${BASE}${path}`);

    if (!res.ok) {
        const text = await res.text().catch(() => "");
        throw new Error(`API ${path} failed: ${res.status} ${text}`);
    }

    return res.json();
}

export async function getTransfers() {
    return request("/api/transfers");
}

export async function getLatestTransfers() {
    return request("/api/transfers");
}

export async function searchNews(keyword) {
    const q = encodeURIComponent(keyword || "");
    return request(`/api/news/search?keyword=${q}`);
}

export async function getNewsById(id) {
    return request(`/api/news/${encodeURIComponent(id)}`);
}

export default {
    getTransfers,
    getLatestTransfers,
    searchNews,
    getNewsById,
};