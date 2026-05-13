# Biblioteca - Sistema de Gestión Bibliotecaria

Sistema de gestión bibliotecaria desarrollado en C# y WinUI 3. Permite administrar libros, usuarios y préstamos mediante una interfaz moderna y dinámica.

## Características

* Gestión de libros

  * Registro, edición y eliminación de libros
  * Control de disponibilidad
  * Búsqueda y filtrado

* Gestión de usuarios

  * Roles de administrador y lector
  * Control de permisos
  * Usuarios activos e inactivos

* Gestión de préstamos

  * Registro de préstamos
  * Devolución de libros
  * Control de préstamos vencidos
  * Historial de préstamos
  * Validación de límites de préstamos

* Interfaz moderna

  * Diseño personalizado con WinUI 3
  * Animaciones y efectos visuales
  * Notificaciones tipo toast
  * Filtros dinámicos y búsqueda en tiempo real

## Tecnologías utilizadas

* C#
* WinUI 3
* XAML
* .NET
* JSON para almacenamiento de datos

## Estructura del proyecto

```text
Biblioteca/
│
├── Models/        # Modelos de datos
├── Services/      # Lógica y manejo de datos
├── Views/         # Interfaces XAML
├── Assets/        # Recursos gráficos
└── Data/          # Archivos JSON
```

## Funcionalidades principales

### Administrador

* Gestionar libros
* Gestionar usuarios
* Registrar préstamos
* Confirmar devoluciones
* Eliminar préstamos
* Consultar historial

### Lector

* Consultar libros
* Ver préstamos propios
* Buscar libros disponibles

## Instalación

1. Clonar el repositorio

```bash
git clone https://github.com/usuario/repositorio.git
```

2. Abrir la solución en Visual Studio

3. Restaurar paquetes NuGet

4. Ejecutar el proyecto

## Requisitos

* Visual Studio 2022
* .NET SDK
* Windows App SDK
* Windows 10 u 11

## Autor

Proyecto desarrollado con C# y WinUI 3 como práctica de desarrollo de aplicaciones de escritorio.
