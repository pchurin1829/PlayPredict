import { Link } from 'react-router-dom'
import './LegalDocumentPage.css'

type Props = { document: 'terms' | 'privacy' }

export default function LegalDocumentPage({ document }: Props) {
  const terms = document === 'terms'
  return (
    <main className="pp-legal">
      <header className="pp-legal__header">
        <Link to="/login">PlayPredict · Copa El Nene</Link>
        <span>BORRADOR PARA REVISIÓN LEGAL — NO APROBADO</span>
      </header>
      <article className="pp-legal__content">
        <p className="pp-legal__status">Documento provisional. No constituye una versión aprobada ni definitiva.</p>
        <h1>{terms ? 'Términos y Condiciones' : 'Política de Privacidad'}</h1>
        <p>Copa El Nene · Versión borrador 0.1 · 27/10/2026</p>
        {terms ? <>
          <h2>Alcance del borrador</h2>
          <p>Este texto preliminar describe el uso de la aplicación para realizar pronósticos deportivos y participar en ligas y clasificaciones. Sus reglas y condiciones particulares deberán publicarse para cada competencia.</p>
          <h2>Participación y edad</h2>
          <p>Los requisitos de edad están pendientes de definición y aprobación jurídica. La eventual declaración de mayoría de edad no verifica documentalmente la edad ni la identidad.</p>
          <h2>Registro y uso de la aplicación</h2>
          <p>El borrador contempla el uso de una cuenta personal, el resguardo de credenciales y la prohibición de suplantación, fraude o manipulación de pronósticos y clasificaciones.</p>
          <h2>Reglas, premios y disponibilidad</h2>
          <p>Las reglas de puntuación, desempates, cierres, correcciones, reclamos, premios y condiciones de servicio deben ser confirmadas antes de aprobar este documento. Este borrador no presume que existan premios ni contraprestaciones.</p>
          <h2>Datos y cambios</h2>
          <p>El tratamiento de datos se describe en la Política de Privacidad. Las condiciones definitivas, su vigencia, los canales de contacto y cualquier mecanismo de aceptación posterior requieren revisión jurídica.</p>
          <p className="pp-legal__pending">Pendiente de aprobación: identificación del organizador y sus datos, requisitos de edad, reglas completas de competencia, premios, reclamos, baja, jurisdicción y demás puntos indicados en el borrador interno.</p>
        </> : <>
          <h2>Responsable y datos tratados</h2>
          <p>La identificación del responsable y sus datos de contacto están pendientes de completar. El borrador contempla datos de cuenta, actividad en ligas y pronósticos, preferencias deportivas y registros técnicos; el inventario debe verificarse contra el funcionamiento real de la aplicación.</p>
          <h2>Finalidades</h2>
          <p>En forma preliminar, los datos se utilizarían para administrar cuentas, autenticar participantes, registrar pronósticos, calcular puntuaciones y rankings, prevenir fraude, atender consultas y operar el servicio. Usos promocionales o comerciales requerirían análisis y, cuando corresponda, autorización separada.</p>
          <h2>Visibilidad, proveedores y conservación</h2>
          <p>Están pendientes de definición qué datos aparecen en rankings, los proveedores que intervienen, las eventuales transferencias y los plazos de conservación. No deben inferirse de este borrador datos o prácticas que no hayan sido verificados.</p>
          <h2>Seguridad, derechos y menores</h2>
          <p>Las medidas de seguridad efectivamente aplicadas, el canal y procedimiento para ejercer derechos y la política sobre menores deben completarse y validarse jurídicamente. Una declaración de mayoría de edad no es una verificación documental.</p>
          <h2>Cookies y cambios</h2>
          <p>El uso de cookies, almacenamiento local, analítica y otros identificadores requiere inventario. Cualquier versión definitiva indicará cómo se informan los cambios y cómo contactar al responsable.</p>
          <p className="pp-legal__pending">Pendiente de aprobación: identidad y contacto del responsable, inventario de datos y proveedores, visibilidad de rankings, conservación, derechos, menores, cookies y transferencias.</p>
        </>}
        <p><Link to={terms ? '/politica-de-privacidad' : '/terminos-y-condiciones'}>{terms ? 'Ver Política de Privacidad' : 'Ver Términos y Condiciones'}</Link></p>
        <p><Link to="/register">Volver al registro</Link></p>
      </article>
    </main>
  )
}
