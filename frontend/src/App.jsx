import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import BatteryDetails from './pages/BatteryDetails';
import BatterySettings from './pages/BatterySettings';

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Navigate to="/login" replace />} />
        <Route path="/login" element={<Login />} />
        <Route path="/dashboard" element={<Dashboard />} />
        <Route path="/bms/:deviceId" element={<BatteryDetails />} />
        <Route path="settings" element={<BatterySettings />} />
      </Routes>
    </BrowserRouter>
  );
}