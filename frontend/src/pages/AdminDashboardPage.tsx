import { Link } from 'react-router-dom'

const ADMIN_AREAS = [
  { title: 'Competencias', text: 'Fuentes deportivas reales.', to: '/competitions' },
  { title: 'Equipos y Planteles', text: 'Gestionar equipos, jugadores y planteles disponibles para las competencias.', to: '/admin/teams' },
  { title: 'Fixture / Partidos', text: 'Fechas y encuentros de cada edición.', to: '/admin/fixture' },
  { title: 'Ligas Oficiales', text: 'Productos comerciales de PlayPredict.', to: '/admin/official-leagues' },
  { title: 'Resultados', text: 'Cargar y corregir resultados reales.', to: '/admin/results' },
  { title: 'Rankings', text: 'Consultar rankings generados.', to: '/rankings' },
  { title: 'Configuración', text: 'Puntuación y reglas de juego.', to: '/admin/scoring' },
]

export default function AdminDashboardPage() {
  return (
    <div>
      <div className="admin-header admin-dashboard__header">
        <div>
          <span className="admin-eyebrow">PLAYPREDICT</span>
          <h1>Administración</h1>
          <p className="admin-help">Gestioná el circuito deportivo y las Ligas Oficiales desde un único lugar.</p>
        </div>
      </div>
      <div className="admin-dashboard__grid">
        {ADMIN_AREAS.map((area) => (
          <article key={area.title} className="admin-dashboard__card">
            <div><h2>{area.title}</h2><p>{area.text}</p></div>
            <Link className="btn btn-primary" to={area.to}>Gestionar</Link>
          </article>
        ))}
      </div>
    </div>
  )
}
