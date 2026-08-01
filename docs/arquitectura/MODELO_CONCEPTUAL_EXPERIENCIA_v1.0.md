# MODELO_CONCEPTUAL_EXPERIENCIA_v1.0

## Modelo Conceptual de la Experiencia Configurable

Versión: 1.0

---

# 1. Propósito

Este documento define el concepto central de PlayPredict: la **Experiencia Configurable**.

No describe tablas.

No describe pantallas.

No describe implementación.

Describe qué representa una Experiencia dentro del producto y cómo se relaciona con el resto de los conceptos.

Todas las funcionalidades futuras deberán respetar este modelo conceptual.

---

# 2. ¿Qué es una Experiencia?

Una Experiencia es el producto digital que una organización ofrece a sus usuarios.

Representa un entorno completo de participación alrededor de uno o más eventos deportivos.

La Experiencia define:

- identidad;
- reglas generales;
- configuración por defecto;
- participantes;
- competencias;
- forma de interacción.

Una Experiencia puede vivir durante muchos años y contener múltiples Competencias.

---

# 3. Objetivos

Una Experiencia tiene como objetivos:

- generar participación;
- fidelizar usuarios;
- aumentar la interacción;
- crear comunidades deportivas;
- permitir campañas comerciales;
- administrar múltiples competencias utilizando un único motor.

---

# 4. Actores

Dentro de una Experiencia participan distintos actores.

## Administrador

Diseña y configura la Experiencia.

---

## Organizador

Administra las Competencias y Ediciones.

---

## Jugador

Participa realizando pronósticos y consultando resultados.

---

## Sponsor

Puede asociarse a la Experiencia y participar mediante premios, campañas o acciones comerciales.

---

# 5. Componentes

Conceptualmente una Experiencia está formada por:

- Branding
- Sponsors
- Participantes
- Configuración por defecto
- Competencias

Estos componentes representan el núcleo del producto.

---

# 6. Branding

El Branding define la identidad visual de la Experiencia.

Puede incluir:

- nombre público;
- logo;
- colores;
- imágenes;
- identidad gráfica;
- textos institucionales;
- dominio;
- favicon;
- enlaces institucionales.

El Branding pertenece a la Experiencia y es compartido por todas sus Competencias.

---

# 7. Sponsors

Los Sponsors representan las organizaciones que participan comercialmente.

Pueden:

- aportar premios;
- financiar campañas;
- asociar su imagen;
- participar en acciones promocionales.

Los Sponsors pertenecen a la Experiencia.

---

# 8. Participantes

Los Participantes representan la comunidad que interactúa con la Experiencia.

La Experiencia define:

- quién puede participar;
- cómo se registra;
- qué roles existen;
- restricciones de acceso;
- grupos especiales.

---

# 9. Configuración por Defecto

La Experiencia establece una configuración inicial para todas las Competencias que contiene.

Puede definir, por ejemplo:

- tipos de pronóstico disponibles;
- reglas de puntuación;
- comportamiento del ranking;
- configuración de premios;
- opciones de visibilidad.

Cada Edición podrá utilizar esta configuración o sobrescribirla parcialmente.

---

# 10. Competencias

Una Competencia representa un conjunto de eventos deportivos relacionados.

Ejemplos:

- Liga Profesional
- Copa Libertadores
- Mundial
- Champions League

Todas las Competencias pertenecen a una única Experiencia.

---

# 11. Ediciones

Cada Competencia puede tener múltiples Ediciones.

Ejemplos:

- Clausura 2026
- Libertadores 2026
- Mundial 2026

Una Edición representa una realización concreta de una Competencia.

Cada Edición puede heredar la configuración por defecto de la Experiencia o definir configuraciones propias.

---

# 12. Configuración de la Edición

Una Edición puede personalizar:

- reglas de puntuación;
- tipos de pronóstico;
- premios;
- fechas de apertura;
- fechas de cierre;
- opciones particulares.

La configuración propia tiene prioridad sobre la configuración por defecto.

---

# 13. Fechas

Cada Edición se organiza mediante Fechas.

Las Fechas agrupan Partidos.

Pueden utilizarse también para Rankings y Premios específicos.

---

# 14. Partidos

Los Partidos representan los eventos deportivos sobre los cuales los usuarios realizan pronósticos.

Cada Partido posee:

- participantes deportivos;
- fecha y hora;
- estado;
- resultado oficial.

---

# 15. Pronósticos

Los Jugadores generan Pronósticos sobre los Partidos.

El tipo de pronóstico disponible depende de la configuración de la Edición.

---

# 16. Resultados Oficiales

Cada Partido posee un único Resultado Oficial.

Este resultado constituye la única fuente válida para evaluar los Pronósticos.

---

# 17. Evaluaciones

Las Evaluaciones representan el resultado del Motor de Puntuación.

Cada Evaluación transforma un Pronóstico en una cantidad de puntos según las reglas vigentes para la Edición.

---

# 18. Rankings

Los Rankings ordenan a los Participantes utilizando las Evaluaciones generadas por el Motor de Puntuación.

Los Rankings nunca calculan puntos.

Únicamente ordenan información existente.

---

# 19. Premios

Los Premios pertenecen conceptualmente a la Edición.

Pueden reconocer:

- posiciones generales;
- posiciones por Fecha;
- mayor cantidad de aciertos;
- cualquier otro criterio definido por la configuración.

Los Premios consultan el Ranking.

Nunca modifican el Ranking.

---

# 20. Relaciones Conceptuales

La estructura general del dominio es:

Experiencia

├── Branding

├── Sponsors

├── Participantes

├── Configuración por defecto

└── Competencias

  └── Ediciones

    ├── Configuración propia

    ├── Fechas

    │  └── Partidos

    │    ├── Pronósticos

    │    ├── Resultados

    │    └── Evaluaciones

    ├── Rankings

    └── Premios

---

# 21. Principios del Modelo

Toda Experiencia deberá respetar los siguientes principios:

- configuración antes que programación;
- reutilización antes que duplicación;
- separación de responsabilidades;
- herencia de configuración;
- posibilidad de sobrescritura por Edición;
- independencia entre motores.

---

# 22. Definición Final

Una Experiencia Configurable es el concepto central de PlayPredict.

Representa un producto digital completo, administrado por una organización, que permite crear comunidades deportivas alrededor de una o más Competencias utilizando un conjunto de motores reutilizables y configurables.

Todas las futuras funcionalidades deberán integrarse dentro de este modelo sin alterar sus principios fundamentales.