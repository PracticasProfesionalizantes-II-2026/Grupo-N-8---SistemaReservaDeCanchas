// Módulo de la página de reservas: los datos iniciales llegan serializados desde la vista
// Razor (reservasIniciales, canchasIniciales, etc.); este script renderiza el listado, el
// mini calendario y los modales, y llama a los endpoints de ReservaController vía apiFetch.
(() => {
    // ---------- Datos base (renderizados por el servidor) ----------

    let reservas = Array.isArray(reservasIniciales) ? reservasIniciales : [];
    const canchas = Array.isArray(canchasIniciales) ? canchasIniciales : [];
    const horariosTodos = (Array.isArray(horariosIniciales) ? horariosIniciales : [])
        .filter(h => h.activo)
        .slice()
        .sort((a, b) => a.horaInicio.localeCompare(b.horaInicio));
    // Solo se pueden elegir materiales activos para una reserva nueva; uno dado de baja no
    // debe aparecer como opción aunque tenga stock.
    const materiales = (Array.isArray(materialesIniciales) ? materialesIniciales : []).filter(m => m.activo);

    const MESES = ['Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio', 'Julio',
        'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'];

    // ---------- Estado del formulario de nueva reserva ----------

    let calAnio, calMes;
    let fechaSeleccionada = null;      // { anio, mes, dia }
    let horariosSeleccionados = [];    // lista de cod_Horario, en el orden en que forman el bloque continuo
    let materialesReserva = [];        // { cod_Material, nombre, cantidad, stock }
    let idParaEliminar = null;

    // ---------- Elementos ----------

    const cuerpoTablaReservas = document.getElementById('cuerpoTablaReservas');
    const inputBuscar = document.getElementById('inputBuscarReserva');
    const selectFiltroCancha = document.getElementById('selectFiltroCancha');
    const selectFiltroEstadoReserva = document.getElementById('selectFiltroEstadoReserva');
    const inputFiltroFecha = document.getElementById('inputFiltroFecha');

    const modalNuevaReserva = document.getElementById('modalNuevaReserva');
    const reservaError = document.getElementById('reservaError');

    const selectCanchaReserva = document.getElementById('selectCanchaReserva');
    const duracionCalculada = document.getElementById('duracionCalculada');

    const mesActualLabel = document.getElementById('mesActualLabel');
    const calendarioGrid = document.getElementById('calendarioGrid');
    const btnMesAnterior = document.getElementById('btnMesAnterior');
    const btnMesSiguiente = document.getElementById('btnMesSiguiente');

    const listaHorarios = document.getElementById('listaHorarios');

    const campoDniReserva = document.getElementById('campoDniReserva');
    const campoTelefonoReserva = document.getElementById('campoTelefonoReserva');

    const cuerpoTablaMaterialesReserva = document.getElementById('cuerpoTablaMaterialesReserva');

    const modalAgregarMaterial = document.getElementById('modalAgregarMaterial');
    const selectMaterialReserva = document.getElementById('selectMaterialReserva');
    const campoCantidadMaterial = document.getElementById('campoCantidadMaterial');
    const agregarMaterialError = document.getElementById('agregarMaterialError');

    const modalBajaReserva = document.getElementById('modalBajaReserva');
    const bajaReservaError = document.getElementById('bajaReservaError');

    const modalDetalleReserva = document.getElementById('modalDetalleReserva');
    const detalleReservaTitulo = document.getElementById('detalleReservaTitulo');
    const detalleCancha = document.getElementById('detalleCancha');
    const detalleEstado = document.getElementById('detalleEstado');
    const detalleFechaReserva = document.getElementById('detalleFechaReserva');
    const detalleFechaRegistro = document.getElementById('detalleFechaRegistro');
    const detalleHorario = document.getElementById('detalleHorario');
    const detalleDuracion = document.getElementById('detalleDuracion');
    const detalleDni = document.getElementById('detalleDni');
    const detalleTelefono = document.getElementById('detalleTelefono');
    const cuerpoDetalleMateriales = document.getElementById('cuerpoDetalleMateriales');

    const toast = document.getElementById('toastReserva');

    // ---------- Utilidades ----------

    function mostrarToast(mensaje, esError = false) {
        toast.textContent = mensaje;
        toast.classList.toggle('toast--error', esError);
        toast.classList.add('toast--visible');
        clearTimeout(mostrarToast._t);
        mostrarToast._t = setTimeout(() => toast.classList.remove('toast--visible'), 3000);
    }

    function escapeHtml(texto) {
        const div = document.createElement('div');
        div.textContent = texto ?? '';
        return div.innerHTML;
    }

    function pad2(n) {
        return String(n).padStart(2, '0');
    }

    function claveFecha(anio, mes, dia) {
        return `${anio}-${pad2(mes + 1)}-${pad2(dia)}`;
    }

    function formatearFecha(fechaIso) {
        const f = new Date(fechaIso);
        if (isNaN(f.getTime())) return fechaIso;
        const dia = String(f.getUTCDate()).padStart(2, '0');
        const mes = String(f.getUTCMonth() + 1).padStart(2, '0');
        const anio = f.getUTCFullYear();
        return `${dia}/${mes}/${anio}`;
    }

    // La fecha de registro (a diferencia de la fecha de reserva) sí tiene hora relevante:
    // se guarda con la hora local del servidor en el momento de la creación.
    function formatearFechaHoraLocal(fechaIso) {
        const f = new Date(fechaIso);
        if (isNaN(f.getTime())) return fechaIso;
        const dia = String(f.getDate()).padStart(2, '0');
        const mes = String(f.getMonth() + 1).padStart(2, '0');
        const anio = f.getFullYear();
        const hora = String(f.getHours()).padStart(2, '0');
        const min = String(f.getMinutes()).padStart(2, '0');
        return `${dia}/${mes}/${anio} ${hora}:${min}`;
    }

    function formatearHora(horaStr) {
        if (!horaStr) return '';
        return horaStr.substring(0, 5);
    }

    function rangoHorario(horarios) {
        if (!horarios || horarios.length === 0) return '-';
        const inicio = formatearHora(horarios[0].hora_Inicio);
        const fin = formatearHora(horarios[horarios.length - 1].hora_Fin);
        return `${inicio} - ${fin}`;
    }

    function fechaReservaKey(reserva) {
        return (reserva.fechaReserva || '').substring(0, 10);
    }

    function horariosDeLaCanchaSeleccionada() {
        const canchaId = parseInt(selectCanchaReserva.value, 10);
        const cancha = canchas.find(c => c.cod_Cancha === canchaId);
        const idsHabilitados = new Set(cancha ? cancha.cod_Horarios : []);
        return horariosTodos.filter(h => idsHabilitados.has(h.cod_Horario));
    }

    // ---------- Listado de reservas ----------

    function renderizarReservas() {
        const texto = inputBuscar.value.trim().toLowerCase();
        const canchaFiltro = selectFiltroCancha.value;
        const estadoFiltro = selectFiltroEstadoReserva.value; // '', 'Pendiente', 'Confirmada', 'Cancelada'
        const fechaFiltro = inputFiltroFecha.value; // '' o 'yyyy-MM-dd'

        const filtradas = reservas.filter(r => {
            const coincideCancha = !canchaFiltro || String(r.cod_Cancha) === canchaFiltro;
            const coincideEstado = !estadoFiltro || r.estado === estadoFiltro;
            const coincideFecha = !fechaFiltro || fechaReservaKey(r) === fechaFiltro;
            if (!coincideCancha || !coincideEstado || !coincideFecha) return false;

            if (!texto) return true;
            // "#12" busca exactamente la reserva N° 12 (así la cita el mensaje de horarios en uso).
            if (texto.startsWith('#')) return String(r.cod_Reserva) === texto.slice(1);
            const etiqueta = `${r.cod_Reserva} ${r.nombre_Cancha} ${r.dni_Cliente} ${r.telefono_Cliente}`.toLowerCase();
            return etiqueta.includes(texto);
        });

        if (filtradas.length === 0) {
            cuerpoTablaReservas.innerHTML = '<tr class="reservas-vacio-fila"><td colspan="7">No se encontraron reservas.</td></tr>';
            return;
        }

        cuerpoTablaReservas.innerHTML = filtradas.map(r => {
            const claseEstado = `reservas-estado--${r.estado.toLowerCase()}`;
            const etiquetaEstado = r.estado === 'Confirmada' ? 'Realizada' : r.estado;
            // Solo se puede cancelar una reserva mientras esté Pendiente (todavía no ocurrió).
            const accion = r.estado === 'Pendiente'
                ? `<button type="button" class="btn-eliminar-fila" data-accion="eliminar" data-id="${r.cod_Reserva}" title="Cancelar reserva">✕</button>`
                : '';

            return `
            <tr data-id="${r.cod_Reserva}">
                <td>#${r.cod_Reserva}</td>
                <td>${escapeHtml(r.nombre_Cancha)}</td>
                <td>${r.duracion}</td>
                <td>${rangoHorario(r.horarios)}</td>
                <td>
                    ${formatearFecha(r.fechaReserva)}
                    <span class="reservas-table__fecha-registro">Registrada: ${formatearFechaHoraLocal(r.fecha)}</span>
                </td>
                <td><span class="reservas-estado ${claseEstado}">${escapeHtml(etiquetaEstado)}</span></td>
                <td class="reservas-table__acciones">
                    ${accion}
                </td>
            </tr>`;
        }).join('');
    }

    // ---------- Mini calendario ----------

    function esFechaPasada(anio, mes, dia) {
        const hoy = new Date();
        hoy.setHours(0, 0, 0, 0);
        const candidata = new Date(anio, mes, dia);
        candidata.setHours(0, 0, 0, 0);
        return candidata.getTime() < hoy.getTime();
    }

    function renderizarCalendario() {
        mesActualLabel.textContent = `${MESES[calMes]} ${calAnio}`;

        const primerDiaMes = new Date(calAnio, calMes, 1);
        const diaSemanaInicio = primerDiaMes.getDay();
        const diasEnMes = new Date(calAnio, calMes + 1, 0).getDate();
        const diasMesAnterior = new Date(calAnio, calMes, 0).getDate();

        const hoy = new Date();
        const esMesActual = hoy.getFullYear() === calAnio && hoy.getMonth() === calMes;

        const celdas = [];

        for (let i = diaSemanaInicio - 1; i >= 0; i--) {
            celdas.push({ dia: diasMesAnterior - i, otroMes: true, mes: calMes - 1 });
        }
        for (let dia = 1; dia <= diasEnMes; dia++) {
            celdas.push({ dia, otroMes: false, mes: calMes });
        }
        while (celdas.length % 7 !== 0) {
            celdas.push({ dia: celdas.length - (diaSemanaInicio + diasEnMes) + 1, otroMes: true, mes: calMes + 1 });
        }

        calendarioGrid.innerHTML = celdas.map(c => {
            if (c.otroMes) {
                return `<button type="button" class="mini-calendario__dia mini-calendario__dia--otro-mes" disabled>${c.dia}</button>`;
            }

            const pasado = esFechaPasada(calAnio, calMes, c.dia);
            const esHoy = esMesActual && hoy.getDate() === c.dia;
            const seleccionado = fechaSeleccionada
                && fechaSeleccionada.anio === calAnio
                && fechaSeleccionada.mes === calMes
                && fechaSeleccionada.dia === c.dia;

            const clases = ['mini-calendario__dia'];
            if (esHoy) clases.push('mini-calendario__dia--hoy');
            if (seleccionado) clases.push('mini-calendario__dia--seleccionado');

            return `<button type="button" class="${clases.join(' ')}" data-dia="${c.dia}" ${pasado ? 'disabled' : ''}>${c.dia}</button>`;
        }).join('');
    }

    function irMesAnterior() {
        calMes -= 1;
        if (calMes < 0) { calMes = 11; calAnio -= 1; }
        renderizarCalendario();
    }

    function irMesSiguiente() {
        calMes += 1;
        if (calMes > 11) { calMes = 0; calAnio += 1; }
        renderizarCalendario();
    }

    function seleccionarDia(dia) {
        reservaError.textContent = '';
        fechaSeleccionada = { anio: calAnio, mes: calMes, dia };
        horariosSeleccionados = [];
        renderizarCalendario();
        renderizarHorarios();
        actualizarDuracion();
    }

    // ---------- Lista de horarios (selección de bloques contiguos) ----------

    function obtenerHorariosOcupados() {
        const canchaId = parseInt(selectCanchaReserva.value, 10);
        if (!canchaId || !fechaSeleccionada) return new Set();

        const fechaStr = claveFecha(fechaSeleccionada.anio, fechaSeleccionada.mes, fechaSeleccionada.dia);
        const ocupados = new Set();
        reservas
            .filter(r => r.cod_Cancha === canchaId && r.estado !== 'Cancelada' && fechaReservaKey(r) === fechaStr)
            .forEach(r => (r.horarios || []).forEach(h => ocupados.add(h.cod_Horario)));

        return ocupados;
    }

    // Si la fecha elegida es hoy, un bloque cuya hora de inicio ya pasó no se puede tomar
    // (misma regla que aplica la API: HoraInicio <= DateTime.Now.TimeOfDay).
    function horarioYaPaso(horaInicioStr) {
        if (!fechaSeleccionada) return false;
        const ahora = new Date();
        const esHoy = fechaSeleccionada.anio === ahora.getFullYear()
            && fechaSeleccionada.mes === ahora.getMonth()
            && fechaSeleccionada.dia === ahora.getDate();
        if (!esHoy) return false;

        const [h, m, s] = horaInicioStr.split(':').map(Number);
        const inicio = new Date(ahora);
        inicio.setHours(h, m || 0, s || 0, 0);
        return inicio.getTime() <= ahora.getTime();
    }

    function renderizarHorarios() {
        const horariosCancha = horariosDeLaCanchaSeleccionada();

        if (horariosCancha.length === 0) {
            listaHorarios.innerHTML = '<div class="horarios-vacio">Esta cancha no tiene horarios configurados.</div>';
            return;
        }

        if (!fechaSeleccionada) {
            listaHorarios.innerHTML = '<div class="horarios-vacio">Seleccioná una fecha en el calendario.</div>';
            return;
        }

        const ocupados = obtenerHorariosOcupados();

        listaHorarios.innerHTML = horariosCancha.map(h => {
            const ocupado = ocupados.has(h.cod_Horario);
            const pasado = !ocupado && horarioYaPaso(h.horaInicio);
            const seleccionado = horariosSeleccionados.includes(h.cod_Horario);

            const clases = ['horario-row'];
            if (ocupado) clases.push('horario-row--ocupado');
            if (pasado) clases.push('horario-row--pasado');
            if (seleccionado) clases.push('horario-row--seleccionado');

            return `
                <div class="${clases.join(' ')}" data-id="${h.cod_Horario}" data-ocupado="${ocupado}" data-pasado="${pasado}" ${pasado ? 'title="Horario ya pasado"' : ''}>
                    <span class="horario-row__label">${formatearHora(h.horaInicio)} - ${formatearHora(h.horaFin)}</span>
                    <input type="checkbox" class="horario-row__checkbox" ${(ocupado || seleccionado) ? 'checked' : ''} ${(ocupado || pasado) ? 'disabled' : ''} tabindex="-1">
                </div>`;
        }).join('');
    }

    function actualizarDuracion() {
        duracionCalculada.textContent = `${horariosSeleccionados.length} hs`;
    }

    // La reserva se arma con un conjunto de bloques de 1 hora, todos contiguos.
    // Al hacer clic en un horario disponible se intenta extender el rango actual;
    // si no es posible extenderlo de forma contigua, se reinicia la selección a ese único bloque.
    function alternarHorario(id, ocupado, pasado) {
        if (ocupado || pasado) return;
        reservaError.textContent = '';

        const horariosCancha = horariosDeLaCanchaSeleccionada();
        const indice = horariosCancha.findIndex(h => h.cod_Horario === id);
        if (indice === -1) return;

        if (horariosSeleccionados.includes(id)) {
            // Solo se puede "soltar" desde una de las puntas del rango, para no dejar huecos.
            const primero = horariosSeleccionados[0];
            const ultimo = horariosSeleccionados[horariosSeleccionados.length - 1];
            if (id === primero) {
                horariosSeleccionados = horariosSeleccionados.slice(1);
            } else if (id === ultimo) {
                horariosSeleccionados = horariosSeleccionados.slice(0, -1);
            } else {
                horariosSeleccionados = [id];
            }
        } else if (horariosSeleccionados.length === 0) {
            horariosSeleccionados = [id];
        } else {
            const indicesSeleccionados = horariosSeleccionados
                .map(codHorario => horariosCancha.findIndex(h => h.cod_Horario === codHorario));
            const minIndice = Math.min(...indicesSeleccionados);
            const maxIndice = Math.max(...indicesSeleccionados);

            if (indice === maxIndice + 1 || indice === minIndice - 1) {
                // Adyacente a una punta: extiende el rango.
                const nuevoMin = Math.min(minIndice, indice);
                const nuevoMax = Math.max(maxIndice, indice);
                horariosSeleccionados = horariosCancha.slice(nuevoMin, nuevoMax + 1).map(h => h.cod_Horario);
            } else {
                // No es contiguo con la selección actual: se reinicia.
                horariosSeleccionados = [id];
            }
        }

        renderizarHorarios();
        actualizarDuracion();
    }

    // ---------- Materiales de la reserva ----------

    function poblarSelectMateriales() {
        selectMaterialReserva.innerHTML = materiales
            .map(m => `<option value="${m.cod_Material}">${escapeHtml(m.nombre)} (Stock: ${m.cant_Material})</option>`)
            .join('');
    }

    function renderizarTablaMateriales() {
        if (materialesReserva.length === 0) {
            cuerpoTablaMaterialesReserva.innerHTML = '<tr class="materiales-vacio"><td colspan="3">Sin materiales</td></tr>';
            return;
        }

        cuerpoTablaMaterialesReserva.innerHTML = materialesReserva.map(item => `
            <tr data-cod-material="${item.cod_Material}">
                <td>${escapeHtml(item.nombre)}</td>
                <td>${item.cantidad}</td>
                <td>
                    <button type="button" class="btn-quitar-item" data-cod-material="${item.cod_Material}" title="Quitar">✕</button>
                </td>
            </tr>`
        ).join('');
    }

    function agregarOActualizarMaterial(codMaterial, cantidad) {
        const material = materiales.find(m => m.cod_Material === codMaterial);
        if (!material) return;

        const existente = materialesReserva.find(i => i.cod_Material === codMaterial);
        if (existente) {
            existente.cantidad += cantidad;
        } else {
            materialesReserva.push({
                cod_Material: codMaterial,
                nombre: material.nombre,
                cantidad
            });
        }

        renderizarTablaMateriales();
    }

    // ---------- Modal Nueva Reserva ----------

    function poblarSelectCanchas() {
        selectCanchaReserva.innerHTML = canchas
            .map(c => `<option value="${c.cod_Cancha}" ${c.estado !== 'Disponible' ? 'disabled' : ''}>${escapeHtml(c.nombre)}${c.estado !== 'Disponible' ? ` (${c.estado})` : ''}</option>`)
            .join('');

        const primeraDisponible = canchas.find(c => c.estado === 'Disponible');
        if (primeraDisponible) {
            selectCanchaReserva.value = primeraDisponible.cod_Cancha;
        }
    }

    function abrirModalNuevaReserva() {
        reservaError.textContent = '';

        const hoy = new Date();
        calAnio = hoy.getFullYear();
        calMes = hoy.getMonth();
        fechaSeleccionada = { anio: calAnio, mes: calMes, dia: hoy.getDate() };
        horariosSeleccionados = [];
        materialesReserva = [];

        campoDniReserva.value = '';
        campoTelefonoReserva.value = '';

        poblarSelectCanchas();
        renderizarCalendario();
        renderizarHorarios();
        renderizarTablaMateriales();
        actualizarDuracion();

        modalNuevaReserva.hidden = false;
    }

    function cerrarModalNuevaReserva() {
        modalNuevaReserva.hidden = true;
    }

    async function confirmarReserva() {
        reservaError.textContent = '';

        const codCancha = parseInt(selectCanchaReserva.value, 10);
        const dni = campoDniReserva.value.trim();
        const telefono = campoTelefonoReserva.value.trim();

        if (!codCancha) {
            reservaError.textContent = 'Seleccioná una cancha disponible';
            return;
        }
        if (!fechaSeleccionada) {
            reservaError.textContent = 'Seleccioná una fecha en el calendario';
            return;
        }
        if (horariosSeleccionados.length === 0) {
            reservaError.textContent = 'Seleccioná al menos un horario';
            return;
        }
        if (!dni) {
            reservaError.textContent = 'Ingresá el DNI de la persona';
            return;
        }
        if (!/^\d{7,8}$/.test(dni)) {
            reservaError.textContent = 'Dni inválido.';
            return;
        }
        if (!telefono) {
            reservaError.textContent = 'Ingresá el teléfono de contacto';
            return;
        }
        if (!/^\d{6,15}$/.test(telefono)) {
            reservaError.textContent = 'Teléfono inválido.';
            return;
        }

        const request = {
            fechaReserva: `${claveFecha(fechaSeleccionada.anio, fechaSeleccionada.mes, fechaSeleccionada.dia)}T00:00:00`,
            dni_Cliente: dni,
            telefono_Cliente: telefono,
            cod_Cancha: codCancha,
            cod_Horarios: horariosSeleccionados,
            materiales: materialesReserva.map(i => ({ cod_Material: i.cod_Material, cantidad: i.cantidad }))
        };

        try {
            const respuesta = await apiFetch('/Reserva/Create', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(request)
            });

            const datos = await respuesta.json();

            if (!respuesta.ok) {
                reservaError.textContent = datos.mensaje || 'No se pudo registrar la reserva';
                return;
            }

            reservas.push(datos);
            // El stock mostrado en el selector de materiales debe reflejar lo recién usado.
            ajustarStockMateriales(materialesReserva, -1);
            cerrarModalNuevaReserva();
            renderizarReservas();
            mostrarToast('Reserva registrada correctamente');
        } catch (ex) {
            reservaError.textContent = 'Error de conexión con el servidor';
        }
    }

    // Aplica en el array local `materiales` la diferencia de stock que ya se reflejó en la API
    // (signo -1 al usar materiales en una reserva nueva, +1 al liberarlos al cancelar).
    function ajustarStockMateriales(items, signo) {
        (items || []).forEach(item => {
            const material = materiales.find(m => m.cod_Material === item.cod_Material);
            if (material) material.cant_Material += signo * item.cantidad;
        });
    }

    // ---------- Submodal Añadir Material ----------

    function abrirModalAgregarMaterial() {
        agregarMaterialError.textContent = '';
        poblarSelectMateriales();
        campoCantidadMaterial.value = 1;
        modalAgregarMaterial.hidden = false;
    }

    function cerrarModalAgregarMaterial() {
        modalAgregarMaterial.hidden = true;
    }

    function confirmarAgregarMaterial() {
        agregarMaterialError.textContent = '';

        const codMaterial = parseInt(selectMaterialReserva.value, 10);
        const cantidad = parseInt(campoCantidadMaterial.value, 10);

        if (!codMaterial) {
            agregarMaterialError.textContent = 'Seleccioná un material';
            return;
        }
        if (!cantidad || cantidad <= 0) {
            agregarMaterialError.textContent = 'La cantidad debe ser mayor a 0';
            return;
        }

        const material = materiales.find(m => m.cod_Material === codMaterial);
        const yaAgregado = materialesReserva.find(i => i.cod_Material === codMaterial)?.cantidad ?? 0;

        if (material && (yaAgregado + cantidad) > material.cant_Material) {
            agregarMaterialError.textContent = `Stock insuficiente para "${material.nombre}". Disponible: ${material.cant_Material}`;
            return;
        }

        agregarOActualizarMaterial(codMaterial, cantidad);
        reservaError.textContent = '';
        cerrarModalAgregarMaterial();
    }

    // ---------- Modal Ver Detalle ----------

    async function abrirModalDetalleReserva(id) {
        try {
            const respuesta = await apiFetch(`/Reserva/Details/${id}`);
            if (!respuesta.ok) {
                mostrarToast('No se pudo obtener el detalle de la reserva', true);
                return;
            }

            const r = await respuesta.json();

            detalleReservaTitulo.textContent = `Reserva #${r.cod_Reserva}`;
            detalleCancha.textContent = r.nombre_Cancha;
            detalleEstado.textContent = r.estado;
            detalleFechaReserva.textContent = formatearFecha(r.fechaReserva);
            detalleFechaRegistro.textContent = formatearFechaHoraLocal(r.fecha);
            detalleHorario.textContent = rangoHorario(r.horarios);
            detalleDuracion.textContent = `${r.duracion} hs`;
            detalleDni.textContent = r.dni_Cliente;
            detalleTelefono.textContent = r.telefono_Cliente;

            const materialesReservados = r.materiales || [];
            cuerpoDetalleMateriales.innerHTML = materialesReservados.length > 0
                ? materialesReservados.map(m => `
                    <tr>
                        <td>${escapeHtml(m.nombre_Material)}</td>
                        <td>${m.cantidad}</td>
                    </tr>`).join('')
                : '<tr class="materiales-vacio"><td colspan="2">Sin materiales</td></tr>';

            modalDetalleReserva.hidden = false;
        } catch (ex) {
            mostrarToast('Error de conexión con el servidor', true);
        }
    }

    function cerrarModalDetalleReserva() {
        modalDetalleReserva.hidden = true;
    }

    // ---------- Modal Cancelar Reserva ----------

    function abrirModalBajaReserva(id) {
        idParaEliminar = id;
        bajaReservaError.textContent = '';
        modalBajaReserva.hidden = false;
    }

    function cerrarModalBajaReserva() {
        modalBajaReserva.hidden = true;
        idParaEliminar = null;
    }

    async function confirmarBajaReserva() {
        if (idParaEliminar == null) return;
        bajaReservaError.textContent = '';

        try {
            const respuesta = await apiFetch(`/Reserva/Delete/${idParaEliminar}`, { method: 'POST' });
            const datos = await respuesta.json();

            if (!respuesta.ok) {
                bajaReservaError.textContent = datos.mensaje || 'No se pudo cancelar la reserva';
                return;
            }

            // La reserva se mantiene en la lista con su nuevo estado (Cancelada) en vez de
            // desaparecer; el detalle de materiales viene en la respuesta para reponer el stock.
            if (datos.reserva) {
                reservas = reservas.map(r => r.cod_Reserva === idParaEliminar ? datos.reserva : r);
                ajustarStockMateriales(datos.reserva.materiales, +1);
            } else {
                reservas = reservas.map(r => r.cod_Reserva === idParaEliminar ? { ...r, estado: 'Cancelada' } : r);
            }
            cerrarModalBajaReserva();
            renderizarReservas();
            mostrarToast('Reserva cancelada');
        } catch (ex) {
            bajaReservaError.textContent = 'Error de conexión con el servidor';
        }
    }

    // ---------- Eventos ----------

    document.getElementById('btnAbrirReserva').addEventListener('click', abrirModalNuevaReserva);
    document.getElementById('btnCancelarReserva').addEventListener('click', cerrarModalNuevaReserva);
    document.getElementById('btnConfirmarReserva').addEventListener('click', confirmarReserva);

    document.getElementById('btnAbrirAgregarMaterial').addEventListener('click', abrirModalAgregarMaterial);
    document.getElementById('btnCancelarAgregarMaterial').addEventListener('click', cerrarModalAgregarMaterial);
    document.getElementById('btnConfirmarAgregarMaterial').addEventListener('click', confirmarAgregarMaterial);

    document.getElementById('btnCancelarBajaReserva').addEventListener('click', cerrarModalBajaReserva);
    document.getElementById('btnConfirmarBajaReserva').addEventListener('click', confirmarBajaReserva);

    btnMesAnterior.addEventListener('click', irMesAnterior);
    btnMesSiguiente.addEventListener('click', irMesSiguiente);

    calendarioGrid.addEventListener('click', (evento) => {
        const boton = evento.target.closest('.mini-calendario__dia:not(:disabled)');
        if (!boton || !boton.dataset.dia) return;
        seleccionarDia(parseInt(boton.dataset.dia, 10));
    });

    selectCanchaReserva.addEventListener('change', () => {
        reservaError.textContent = '';
        horariosSeleccionados = [];
        renderizarHorarios();
        actualizarDuracion();
    });

    listaHorarios.addEventListener('click', (evento) => {
        const fila = evento.target.closest('.horario-row');
        if (!fila) return;
        alternarHorario(parseInt(fila.dataset.id, 10), fila.dataset.ocupado === 'true');
    });

    cuerpoTablaMaterialesReserva.addEventListener('click', (evento) => {
        const boton = evento.target.closest('.btn-quitar-item');
        if (!boton) return;
        reservaError.textContent = '';
        const codMaterial = parseInt(boton.dataset.codMaterial, 10);
        materialesReserva = materialesReserva.filter(i => i.cod_Material !== codMaterial);
        renderizarTablaMateriales();
    });

    cuerpoTablaReservas.addEventListener('click', (evento) => {
        const boton = evento.target.closest('button[data-accion="eliminar"]');
        if (boton) {
            abrirModalBajaReserva(parseInt(boton.dataset.id, 10));
            return;
        }

        const fila = evento.target.closest('tr[data-id]');
        if (fila) {
            abrirModalDetalleReserva(parseInt(fila.dataset.id, 10));
        }
    });

    document.getElementById('btnCerrarDetalleReserva').addEventListener('click', cerrarModalDetalleReserva);

    // El error del modal no debe quedar visible después de que el usuario corrige el dato.
    campoDniReserva.addEventListener('input', () => { reservaError.textContent = ''; });
    campoTelefonoReserva.addEventListener('input', () => { reservaError.textContent = ''; });

    inputBuscar.addEventListener('input', renderizarReservas);
    selectFiltroCancha.addEventListener('change', renderizarReservas);
    selectFiltroEstadoReserva.addEventListener('change', renderizarReservas);
    inputFiltroFecha.addEventListener('change', renderizarReservas);

    [modalNuevaReserva, modalAgregarMaterial, modalBajaReserva, modalDetalleReserva].forEach(overlay => {
        overlay.addEventListener('click', (evento) => {
            if (evento.target === overlay) overlay.hidden = true;
        });
    });

    // ---------- Inicio ----------

    renderizarReservas();
})();
