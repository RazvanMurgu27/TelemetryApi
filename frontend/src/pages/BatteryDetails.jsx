import { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import BatteryChart from "./BatteryChart";

/**
 * Componenta Smart (Container) pentru afisarea detaliata a telemetriei unei baterii.
 * Responsabilitati:
 * 1. Extragerea securizata a datelor istorice din API-ul .NET Core folosind JWT Bearer Token.
 * 2. Gestionarea starilor componentei (Loading, Empty State, Error Handling).
 * 3. Transmiterea datelor (Props) catre componenta de prezentare BatteryChart.
 * 4. Randarea tabulara a log-urilor brute cu highlight pentru erorile raportate de BMS Logic.
 */
export default function BatteryDetails() {
    const { deviceId } = useParams();
    const navigate = useNavigate();

    const [telemetry, setTelemetry] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        /**
         * Functie asincrona pentru extragerea datelor de telemetrie de la server.
         * Implementeaza validarea token-ului de securitate si tratarea exceptiilor de retea.
         */
        const fetchTelemetry = async () => {
            try {
                const token = localStorage.getItem("token");
                
                // Guard clause: Daca nu exista token, redirectioneaza imediat la login
                if (!token) {
                    navigate('/login');
                    return;
                }

                const API_URL = import.meta.env.VITE_API_URL;
                const response = await fetch(`${API_URL}/api/telemetry/battery/${deviceId}`, {
                    method: "GET",
                    headers: {
                        "Content-Type": "application/json",
                        "Authorization": `Bearer ${token}`
                    }
                });

                if (response.ok) {
                    const data = await response.json();
                    setTelemetry(data);
                } else if (response.status === 401) {
                    // Daca token-ul a expirat (401 Unauthorized), fortam re-autentificarea
                    console.error("Token expirat sau invalid. Redirectionare la Login.");
                    localStorage.removeItem("token");
                    navigate('/login');
                } else {
                    console.error("Eroare de la API. Status:", response.status);
                }
            } catch (error) {
                console.error("Eroare critica la fetch (Offline sau CORS):", error);
            } finally {
                setLoading(false);
            }
        };

        fetchTelemetry();
    }, [deviceId, navigate]);

    return (
        <div className="min-h-screen bg-slate-900 p-8 font-sans">
            {/* Bara superioara de navigare (Top Navigation Bar) */}
            <div className="flex items-center gap-4 mb-8">
                <button 
                    onClick={() => navigate('/dashboard')}
                    className="text-slate-400 hover:text-emerald-400 transition-colors flex items-center gap-2 px-4 py-3 bg-slate-800 rounded-lg border border-slate-700 hover:border-emerald-400"
                >
                    <span>&larr;</span> Inapoi la pagina principala
                </button>
                <div>
                    <h1 className="text-3xl font-bold text-white">
                        Telemetrie BMS: <span className="text-emerald-400">{deviceId}</span>
                    </h1>
                </div>
            </div>

            {/* View Management: Tratam randarea conditionata bazata pe stadiul request-ului */}
            {loading ? (
                <div className="text-emerald-500 text-xl text-center mt-20 animate-pulse">
                    Se incarca datele securizate...
                </div>
            ) : telemetry.length === 0 ? (
                <div className="bg-slate-800 p-8 rounded-lg border border-slate-700 text-center mt-10">
                    <h2 className="text-2xl text-slate-300 mb-2">Nu exista date inca</h2>
                    <p className="text-slate-500">Nu au fost transmise date pentru aceasta baterie.</p>
                </div>
            ) : (
                <>
                    {/* Widget Principal: Progres SOC (State of Charge) al celui mai recent pachet */}
                    {telemetry[0]?.soc != null && (
                        <div className="bg-slate-800 p-6 rounded-lg border border-slate-700 mb-8 shadow-xl">
                            <h2 className="text-slate-400 mb-4 font-semibold uppercase tracking-wider text-sm flex items-center gap-2">
                                <span>⚡️</span> Nivel Incarcare (SOC)
                            </h2>
                            <div className="flex items-center gap-6">
                                <div className="text-4xl font-bold text-white tabular-nums w-24">
                                    {telemetry[0].soc}%
                                </div>
                                <div className="flex-1 h-8 bg-slate-900 rounded-full overflow-hidden border border-slate-700 shadow-inner">
                                    {/* Randare conditionata a culorii barei de progres bazata pe procente critice */}
                                    <div
                                        className={`h-full transition-all duration-1000 ${
                                            telemetry[0].soc <= 20 ? 'bg-red-500' :
                                            telemetry[0].soc <= 50 ? 'bg-yellow-400' :
                                            'bg-emerald-500'
                                        }`}
                                        style={{ width: `${telemetry[0].soc}%` }}
                                    />
                                </div>
                            </div>
                        </div>
                    )}

                    {/* Injectarea componentei grafice cu array-ul complet de date */}
                    <BatteryChart telemetry={telemetry} />

                    {/* Tabelul Datagramelor de Telemetrie Bruta */}
                    <div className="bg-slate-800 rounded-lg border border-slate-700 overflow-hidden shadow-xl">
                        <div className="overflow-x-auto">
                            <table className="w-full text-left text-slate-300 min-w-max">
                                <thead className="bg-slate-900 text-emerald-500 font-mono text-sm uppercase">
                                    <tr>
                                        <th className="px-6 py-4 text-left">Data si Ora</th>
                                        <th className="px-6 py-4 text-center">Tensiune (V)</th>
                                        <th className="px-6 py-4 text-center">SOC (%)</th>
                                        <th className="px-6 py-4 text-center">Curent (A)</th>
                                        <th className="px-6 py-4 text-center">Putere (W)</th>
                                        <th className="px-6 py-4 text-center">Temperatura (°C)</th>
                                        <th className="px-6 py-4 text-center">Delta(ΔV)</th>
                                        <th className="px-6 py-4 text-center">Eroare Activa</th>
                                        <th className="px-6 py-4 text-center">Data Eroare</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-slate-700/50">
                                    {telemetry.map((record) => {
                                        // Calculam DeltaV pe rand curent
                                        const deltaV = (record.highestCellVoltage != null && record.lowestCellVoltage != null)
                                            ? (record.highestCellVoltage - record.lowestCellVoltage)
                                            : null;

                                        // Fallback pe timestamp daca lipseste un ID unic din baza de date
                                        const keyID = record.id || record.timestamp;

                                        return (
                                            <tr key={keyID} className="hover:bg-slate-700/30 transition-colors">
                                                <td className="px-6 py-4 text-left text-sm text-slate-400">
                                                    {new Date(record.timestamp).toLocaleString()}
                                                </td>
                                                <td className="px-6 py-4 font-bold text-white text-center tabular-nums">
                                                    {record.voltage != null ? record.voltage.toFixed(2) : "N/A"}
                                                </td>
                                                <td className="px-6 py-4 font-bold text-white text-center tabular-nums">
                                                    {record.soc != null ? `${record.soc}%` : "N/A"}
                                                </td>
                                                <td className="px-6 py-4 text-slate-300 text-center tabular-nums">
                                                    {record.current != null ? record.current.toFixed(2) : "N/A"}
                                                </td>
                                                <td className="px-6 py-4 text-slate-300 text-center tabular-nums">
                                                    {record.power != null ? record.power.toFixed(2) : "N/A"}
                                                </td>
                                                <td className="px-6 py-4 text-slate-300 text-center tabular-nums">
                                                    {record.temperature != null ? record.temperature.toFixed(1) : "N/A"}
                                                </td>
                                                <td className="px-6 py-4 text-emerald-400 font-semibold text-center tabular-nums">
                                                    {deltaV != null ? `${deltaV.toFixed(3)} V` : "N/A"}
                                                </td>
                                                
                                                {/* Evaluarea logica a mesajului de eroare pentru Highlight vizual */}
                                                <td className="px-6 py-4 text-center text-sm">
                                                    {record.errorMessage ? (
                                                        <span className="text-red-400 font-semibold bg-red-900/20 px-2 py-1 rounded">
                                                            {record.errorMessage}
                                                        </span>
                                                    ) : (
                                                        <span className="text-slate-600">-</span>
                                                    )}
                                                </td>
                                                <td className="px-6 py-4 text-left text-sm text-slate-400">
                                                    {record.errorMessage && record.errorTimeStamp
                                                        ? new Date(record.errorTimeStamp).toLocaleString()
                                                        : <span className="text-slate-600">-</span>}
                                                </td>
                                            </tr>
                                        );
                                    })}
                                </tbody>
                            </table>
                        </div>
                    </div>
                </>
            )}
        </div>
    );
}