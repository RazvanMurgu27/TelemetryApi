import { useState } from "react";
import { useNavigate } from "react-router-dom";

export default function Login() {
    const navigate = useNavigate();

    //Setari pentru a controla formularul si erorile
    const [isLoginMode, setIsLoginMode] = useState(true);
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');


    const handleSubmit = async (e) => {
        e.preventDefault(); // previne refresh automat la toata pagina 
        setError(''); //curata memoria de orice eroare anterioara, daca a fost facuta o greseala. mesajul cu rosu dispare la o noua incercare 

        //1.Stabilirea rutei in functie de modul in care ne aflam(Login sau REgister)
        const endpoint = isLoginMode ? '/api/auth/login' : '/api/auth/register';

        const url = `https://localhost:7186${endpoint}`;

        try{
            //2.Trimite cererea catre C#
            const response = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({email:email, password:password}) //Acelasi format folosit in Swagger
            });

            //3.Backend-ul a raspuns cu eroare (Cod 400)
            if(!response.ok)
            {
                //Citim textul erorii generate de C#
                const errorMessage = await response.text();
                setError(errorMessage);
                return;
            }
    
            //4.Succes(Cod 200)
            if(isLoginMode)
            {
                const data = await response.json();

                localStorage.setItem('userId', data.userId);
                localStorage.setItem('token', data.token);

                navigate('/dashboard');
            }
            else
            {
                alert(`Contul a fost creat cu succes pentru adresa: ${email}\n\n ❗️IMPORTANT: Toate alertele de siguranta pentru sistemele tale vor fi trimise pe aceasta adresa de email! Puteti schimba adresa din meniul de setari al fiecarei baterii!`);
                setIsLoginMode(true);
                setPassword('');
            }}
            catch (err){
                //Aceasta eroare apare daca serverul C# este oprit(nu raspunde deloc)
                setError('Eroare de conexiune cu serverul! Verifica daca backend-ul ruleaza!');
            }
        };
    return(
        <div className="min-h-screen bg-slate-900 flex items-center justify-center p-4">
            <div className="bg-slate-800 p-8 rounded-2xl shadow-2xl max-w-md w-full border border_slate-700">
                <h1 className="text-3xl font-bold text-emerald-400 mb-6 text-center">
                    {isLoginMode ? 'Autentificare Sistem' : 'Creare Cont Nou'}
                </h1>

                {/*Afisare erori daca exista */}
                { error &&(
                    <div className="bg-red-500/10 border border-red-500/50 text-red-400 p-3 rounded-lg mb-6 text-sm text-center">
                        {error}
                    </div>
                )}

                <form onSubmit={handleSubmit} className="space-y-5">
                    <div>
                        <label className="block text-slate-300 mb-2 text-sm font-medium">Adresa Email</label>
                        <input
                            type="email"
                            required
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            className="w-full bg bg-slate-900 text-white rounded-lg p-3 border border-slate-700 focus:border-emerald-500 focus:outline-none"
                            placeholder="nume@exemplu.com"
                        /> 
                    </div>
                    <div>
                        <label className="block text-slate-300 mb-2 text-sm font-medium">Parola</label>
                        <input
                            type="password"
                            required
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            className="w-full bg-slate-900 text-white rounded-lg p-3 border border-slate-700 focus:border-emerald-500 focus:outline-none"
                            placeholder="********"
                        />
                    </div>

                    <button type="submit" className="w-full bg-emerald-500 hover:bg-emerald-600 text-white-600 text-white font-semibold py-3 rounded-lg transition-colors mt-4 shadow-lg shadow-emerald-500/20">
                        {isLoginMode ? 'Intra in cont' : 'Inregistreaza-te'}
                    </button>
                </form>
                
                {/*Butonul de comutare intre moduri */}
                <div className="mt-6 text-center border-t border-slate-700 pt-6">
                    <p className="text-slate-400 text-sm mb-2">
                        {isLoginMode ? 'Nu ai sistemul configurat inca?' : 'Ai deja un cont activ?'}
                    </p>
                    <button
                        onClick={() => {
                            setIsLoginMode(!isLoginMode);
                            setError(''); //Curata eroarea la schimbarea paginii
                        }}
                        className="text-emerald-400 hover:text-emerald-300 font-semibold text-sm transition-colors">
                        {isLoginMode ? 'Creeaza un cont nou' : 'Inapoi la autentificare'}
                    </button>
                </div>
            </div>
        </div> 
    );
}