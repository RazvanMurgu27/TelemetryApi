import { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import BatteryChart from "./BatteryChart";

export default function BatteryDetails() {
    const { deviceId } = useParams(); //Extragem Id-ul direct din link
    const navigate = useNavigate();

    const [telemetry, setTelemetry] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const fetchTelemetry = async() => {
            try{
                const token = localStorage.getItem("token");
                //Apelam endpointul
                const response = await fetch(`https://localhost:7186/api/telemetry/battery/${deviceId}`,{
                    method: "GET",
                    headers: {
                        "Content-Type": "application/json",
                        "Authorization": `Bearer ${token}`
                    }
                });
                if(response.ok)
                {
                    const data = await response.json();
                    console.log("Datele primite de la C# sunt:", data);
                    setTelemetry(data);
                }
                else{
                    console.error("Eroare de la C#! Status:", response.status);
                }
            } catch (error) {
                console.error("Eroare la aducerea telemetriei:", error);
            } finally {
                setLoading(false);
            }
        };

        fetchTelemetry();
    }, [deviceId]);

    return(
        <div className="min-h-screen bg-slate-900 p-8 font-sans">
            {/*Bara de sus cu buton de inapoi*/}
            <div className="flex items-center gap-4 mb-8">
                <button 
                    onClick={() => navigate('/dashboard')}
                    className="text-slate-400 hover:text-emerald-400 transition-colors flex items-center gap-2 px-4 py-3 bg-slate-800 rounded-lg border border-slate-700 hover:border-emerald-400"
                >
                    <span>&larr;</span>Inapoi la Dashboard
                </button>
                <div>
                    <h1 className="text-3xl font-bold text-white">
                        Telemetrie BMS:<span className="text-emerald-400">{deviceId}</span>
                    </h1>
                </div>
            </div>

            {loading? (
                <div className="text-emerald-500 text-xl text-center mt-20 animate-pulse">
                    Se incarca datele...
                </div>
            ):telemetry.length == 0 ? (
                <div className="bg-slate-800 p-8 rounded-lg border border-slate-700 text-center mt-10">
                    <h2 className="text-2xl text-slate-300 mb-2">Nu exista date inca</h2>
                    <p className="text-salte-500">Nu au fost trimise date de telemetrie pentru aceasta baterie.</p>
                </div>
            ) : (
                <>
                {/*Widget pentru bara de progres*/}
                    {telemetry[0].soc !==undefined && telemetry[0].soc !== null && (
                        <div className="bg-slate-800 p-6 rounded-lg border border-slate-700 mb-8 shadow-xl">
                            <h2 className="text-slate-400 mb-4 font-semibold uppercase tracking-wider text-sm flex items-center gap-2">
                                <span>⚡️</span>Nivel Incarcare (SOC)
                            </h2>
                            <div className="flex items-center gap-6">
                                {/*Textul cu procentajul*/}
                                <div className="text-4xl font-bold text-white tabular-nums w-24">
                                    {telemetry[0].soc}%
                                </div>

                                {/*Fundalul Barei*/}
                                <div className="flex-1 h-8 bg-slate-900 rounded-full overflow-hidden border border-slate-700 shadow-inner">
                                    {/*Bara colorata care se umple (Latinmea este controlata dinamic)*/}
                                    <div
                                        className={`h-full transition-all duration-1000 ${
                                            telemetry[0].soc <= 20 ? 'bg-red-500' :
                                            telemetry[0].soc <= 50 ?'bg-yellow-400' :
                                            'bg-emerald-500'
                                        }`}
                                        style={{width: `${telemetry[0].soc}%`}}>
                                    </div>
                                </div>
                            </div>
                        </div>

                )}
                <BatteryChart telemetry={telemetry} />
                <div className="bg-slate-800 rounded-lg border border-slate-700 overflow-hidden shadow-xl">
                    {/*Tabel cu datele de telemetrie*/}
                    <div className="overflow-x-auto">
                        <table className="w-full text-left text-slate-300 min-w-max">
                            <thead className=" bg-slate-900 text-emerald-500 font-mono text-sm uppercase">
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
                                {telemetry.map((record, index) => {
                                    //Calculam diferenta de voltaj dintre celule 
                                    const deltaV = (record.highestCellVoltage !== undefined && record.lowestCellVoltage !==undefined)
                                        ?(record.highestCellVoltage - record.lowestCellVoltage)
                                        : null;

                                    return (
                                        <tr key={index} className="hover:bg-slate-700/30 transition-colors">
                                            {/*Data telemetriei*/}
                                            <td className="px-6 py-4 text-left text-sm text-slate-400">
                                                {new Date(record.timestamp + "Z").toLocaleString()}
                                            </td>
                                            {/*VOltak curent temperatura delta*/}
                                            <td className="pxp-6 py-4 font-bold text-white text-center tabular-nums">
                                                {record.voltage !== undefined && record.voltage !== null ? record.voltage.toFixed(2) : "N/A"}
                                            </td>
                                            <td className="px-6 py-4 font-bold text-white text-center tabular-nums">
                                                {record.soc !== undefined && record.soc !== null ? `${record.soc}%` : "N/A"}
                                            </td>
                                            <td className="px-6 py-4 text-slate-300 text-center tabular-nums">
                                                {record.current !== undefined && record.current !== null ? record.current.toFixed(2) : "N/A"}
                                            </td>
                                            <td className="px-6 py-4 text-slate-300 text-center tabular-nums">
                                                {record.power !== undefined && record.power !== null ? record.power.toFixed(2) : "N/A"}
                                            </td>
                                            <td className="px-6 py-4 text-slate-300 text-center tabular-nums">
                                                { record.temperature !== undefined && record.temperature !== null ?  record.temperature.toFixed(1) : "N/A"}
                                            </td>
                                            <td className="px-6 py-4 text-emerald-400 font-semibold text-center tabular-nums">
                                                {deltaV !== null ? `${deltaV.toFixed(3)} V` : "N/A"}
                                            </td>

                                            {/*MEsajul de eroare (cu formatare rosie)*/}
                                            <td className="px-6 py-4 text-center text-sm">
                                                {record.errorMessage?(
                                                    <span className="text-red-400 font-semibold bg-red-900/20 px-2 py-1 rounded">
                                                        {record.errorMessage}
                                                    </span>
                                                ) : (
                                                    <span className="text-slate-600">-</span>
                                                )}
                                            </td>
                                            {/*Data si ora erorii*/}
                                            <td className="px-6 py-4 text-left text-sm text-slate-400">
                                                {record.errorMessage && record.errorTimeStamp
                                                    ? new Date(record.errorTimeStamp + "Z").toLocaleString()
                                                    :<span className="text-slate-600">-</span>}
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