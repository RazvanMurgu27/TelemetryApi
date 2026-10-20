import { 
    LineChart, Line, AreaChart, Area, XAxis, YAxis, 
    CartesianGrid, Tooltip, Legend, ResponsiveContainer 
} from 'recharts';

export default function BatteryChart({ telemetry }) {
    if (!telemetry || telemetry.length === 0) return null;

    const chartData = [...telemetry].reverse().map(record => {
        const high = record.highestCellVoltage;
        const low = record.lowestCellVoltage;
        
        let calcDelta = null;
        if (high != null && low != null) {
            calcDelta = Number(Math.abs(high - low).toFixed(3));
        }

        return {
            ...record,
            timeLabel: new Date(record.timestamp + "Z").toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second:'2-digit' }),
            deltaV: calcDelta
        };
    });

    const tooltipStyle = {
        backgroundColor: '#1e293b',
        borderColor: '#334155',
        borderRadius: '8px',
        color: '#f8fafc'
    };

    return (
        <div className="grid grid-cols-1 xl:grid-cols-2 gap-6 mb-8">
            
            {/* 1. Putere */}
            <div className="bg-slate-800 p-6 rounded-lg border border-slate-700 shadow-xl flex flex-col h-96">
                <h2 className="text-slate-400 mb-6 font-semibold uppercase tracking-wider text-sm flex items-center gap-2">
                    <span>⚡</span> Putere (Tensiune & Curent)
                </h2>
                <div className="flex-1 w-full">
                    <ResponsiveContainer width="100%" height="100%">
                        <LineChart data={chartData} margin={{ top: 5, right: 0, left: -20, bottom: 5 }}>
                            <CartesianGrid strokeDasharray="3 3" stroke="#334155" vertical={false} />
                            <XAxis dataKey="timeLabel" stroke="#94a3b8" tick={{fontSize: 12}} />
                            <YAxis yAxisId="left" stroke="#10b981" domain={['auto', 'auto']} tick={{fontSize: 12}} />
                            <YAxis yAxisId="right" orientation="right" stroke="#3b82f6" domain={['auto', 'auto']} tick={{fontSize: 12}} />
                            <Tooltip contentStyle={tooltipStyle} itemStyle={{ fontWeight: 'bold' }} />
                            <Legend wrapperStyle={{ paddingTop: '10px' }} />
                            <Line yAxisId="left" type="monotone" dataKey="voltage" name="Tensiune (V)" stroke="#10b981" strokeWidth={3} dot={false} activeDot={{ r: 6 }} />
                            <Line yAxisId="right" type="monotone" dataKey="current" name="Curent (A)" stroke="#3b82f6" strokeWidth={3} dot={false} activeDot={{ r: 6 }} />
                        </LineChart>
                    </ResponsiveContainer>
                </div>
            </div>

            {/* 2. SOC */}
            <div className="bg-slate-800 p-6 rounded-lg border border-slate-700 shadow-xl flex flex-col h-96">
                <h2 className="text-slate-400 mb-6 font-semibold uppercase tracking-wider text-sm flex items-center gap-2">
                    <span>🔋</span> Evolutie Incarcare (SOC %)
                </h2>
                <div className="flex-1 w-full">
                    <ResponsiveContainer width="100%" height="100%">
                        <AreaChart data={chartData} margin={{ top: 5, right: 10, left: -20, bottom: 5 }}>
                            <CartesianGrid strokeDasharray="3 3" stroke="#334155" vertical={false} />
                            <XAxis dataKey="timeLabel" stroke="#94a3b8" tick={{fontSize: 12}} />
                            <YAxis stroke="#f59e0b" domain={[0, 100]} tick={{fontSize: 12}} />
                            <Tooltip contentStyle={tooltipStyle} itemStyle={{ fontWeight: 'bold' }} />
                            <Legend wrapperStyle={{ paddingTop: '10px' }} />
                            <Area type="monotone" dataKey="soc" name="SOC (%)" stroke="#f59e0b" fillOpacity={0.3} fill="#f59e0b" strokeWidth={3} activeDot={{ r: 6 }} />
                        </AreaChart>
                    </ResponsiveContainer>
                </div>
            </div>

            {/* 3. Temperatura */}
            <div className="bg-slate-800 p-6 rounded-lg border border-slate-700 shadow-xl flex flex-col h-96">
                <h2 className="text-slate-400 mb-6 font-semibold uppercase tracking-wider text-sm flex items-center gap-2">
                    <span>🌡️</span> Siguranta Termica (°C)
                </h2>
                <div className="flex-1 w-full">
                    <ResponsiveContainer width="100%" height="100%">
                        <LineChart data={chartData} margin={{ top: 5, right: 10, left: -20, bottom: 5 }}>
                            <CartesianGrid strokeDasharray="3 3" stroke="#334155" vertical={false} />
                            <XAxis dataKey="timeLabel" stroke="#94a3b8" tick={{fontSize: 12}} />
                            <YAxis stroke="#ef4444" domain={['auto', 'auto']} tick={{fontSize: 12}} />
                            <Tooltip contentStyle={tooltipStyle} itemStyle={{ fontWeight: 'bold' }} />
                            <Legend wrapperStyle={{ paddingTop: '10px' }} />
                            <Line type="monotone" dataKey="temperature" name="Temperatura (°C)" stroke="#ef4444" strokeWidth={3} dot={false} activeDot={{ r: 6 }} />
                        </LineChart>
                    </ResponsiveContainer>
                </div>
            </div>

            {/* 4. Delta V */}
            <div className="bg-slate-800 p-6 rounded-lg border border-slate-700 shadow-xl flex flex-col h-96">
                <h2 className="text-slate-400 mb-6 font-semibold uppercase tracking-wider text-sm flex items-center gap-2">
                    <span>⚖️</span> Echilibru Celule (Delta V)
                </h2>
                <div className="flex-1 w-full">
                    <ResponsiveContainer width="100%" height="100%">
                        <LineChart data={chartData} margin={{ top: 5, right: 10, left: -20, bottom: 5 }}>
                            <CartesianGrid strokeDasharray="3 3" stroke="#334155" vertical={false} />
                            <XAxis dataKey="timeLabel" stroke="#94a3b8" tick={{fontSize: 12}} />
                            <YAxis 
                                stroke="#06b6d4" 
                                domain={[0, 0.5]} 
                                allowDataOverflow={true} 
                                tickFormatter={(value) => typeof value === 'number' ? value.toFixed(3) : value} 
                                width={65} 
                            />
                            <Tooltip contentStyle={tooltipStyle} itemStyle={{ fontWeight: 'bold' }} />
                            <Legend wrapperStyle={{ paddingTop: '10px' }} />
                            <Line 
                                type="monotone" 
                                dataKey="deltaV" 
                                name="Dezechilibru (V)" 
                                stroke="#06b6d4" 
                                strokeWidth={3} 
                                dot={{ r: 4, fill: "#06b6d4" }} 
                                activeDot={{ r: 8 }} 
                                connectNulls={true} 
                            />
                        </LineChart>
                    </ResponsiveContainer>
                </div>
            </div>

        </div>
    );
}