import axios from 'axios'

const api = axios.create({
  baseURL: 'https://localhost:7176/api',
  timeout: 15000,
})

export default api
