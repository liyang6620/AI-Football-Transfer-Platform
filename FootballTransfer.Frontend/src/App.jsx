import { Routes, Route } from 'react-router-dom'
import Navbar from './components/Navbar'
import Home from './pages/Home'
import Transfers from './pages/Transfers'
import CategoryTransfers from './pages/CategoryTransfers'
import TransferDetail from './pages/TransferDetail'
import Statistics from './pages/Statistics'
import About from './pages/About'

export default function App() {
  return (
    <div>
      <Navbar />
      <main style={{ marginTop: 12 }}>
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/transfers" element={<Transfers />} />
          <Route path="/transfers/:category" element={<CategoryTransfers />} />
          <Route path="/transfer/:id" element={<TransferDetail />} />
          <Route path="/statistics" element={<Statistics />} />
          <Route path="/about" element={<About />} />
        </Routes>
      </main>
    </div>
  )
}
