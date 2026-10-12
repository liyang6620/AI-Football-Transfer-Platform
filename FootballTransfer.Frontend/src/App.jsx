import { Navigate, Route, Routes, useParams } from 'react-router-dom'
import Navbar from './components/Navbar'
import ServiceBanner from './components/ServiceBanner'
import Home from './pages/Home'
import Transfers from './pages/Transfers'
import TransferDetail from './pages/TransferDetail'
import Statistics from './pages/Statistics'
import About from './pages/About'
import './styles/professional.css'

const LEGACY_CATEGORIES = new Set(['official', 'rumours', 'contracts', 'free'])

function LegacyCategoryRedirect() {
  const { category } = useParams()
  const target = LEGACY_CATEGORIES.has(category) ? category : 'official'
  return <Navigate replace to={`/transfers?cat=${target}`} />
}

export default function App() {
  return (
    <div>
      <Navbar />
      <ServiceBanner />
      <main>
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/transfers" element={<Transfers />} />
          <Route path="/transfers/:category" element={<LegacyCategoryRedirect />} />
          <Route path="/transfer/:id" element={<TransferDetail />} />
          <Route path="/statistics" element={<Statistics />} />
          <Route path="/about" element={<About />} />
          <Route path="*" element={<Navigate replace to="/" />} />
        </Routes>
      </main>
    </div>
  )
}
