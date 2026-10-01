// Módulo de la página de estadísticas: los datos iniciales llegan serializados desde la vista
// Razor (productosIniciales, canchasIniciales); este script carga los reportes de ventas y de
// reservas por cancha (día/semana/mes) vía apiFetch y renderiza KPIs, chips de filtro y barras.
(() => {
    const productos = Array.isArray(productosIniciales) ? productosIniciales : [];
    const canchas = Array.isArray(canchasIniciales) ? canchasIniciales : [];

    let periodo = 'dia'; // 'dia' | 'semana' | 'mes'
    let canchasExcluidas = new Set();
    let productosExcluidos = new Set();

    const inputFecha = document.getElementById('inputFecha');
    const botonesPeriodo = document.querySelectorAll('.dash__periodo-btn');

    const kpiReservas = document.getElementById('kpiReservas');
    const kpiIngresos = document.getElementById('kpiIngresos');
    const kpiCantidadVendida = document.getElementById('kpiCantidadVendida');

    const chipsCanchas = document.getElementById('chipsCanchas');
    const chipsProductos = document.getElementById('chipsProductos');

    const barrasCancha = document.getElementById('barrasCancha');
    const barrasProducto = document.getElementById('barrasProducto');
    const canchaVacio = document.getElementById('canchaVacio');
    const productoVacio = document.getElementById('productoVacio');
    const tituloPanelProducto = document.getElementById('tituloPanelProducto');

    // ---------- Utilidades ----------

    function formatearMoneda(valor) {
        return new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', minimumFractionDigits: 0 })
            .format(valor || 0)
            .replace('ARS', '$')
            .replace(/\s/g, '');
    }

    function escapeHtml(texto) {
        const div = document.createElement('div');
        div.textContent = texto ?? '';
        return div.innerHTML;
    }

    function renderizarBarras(contenedor, items, formatearValor) {
        if (items.length === 0) {
            contenedor.innerHTML = '';
            return;
        }
        const maximo = Math.max(...items.map(i => i.valor), 1);

        contenedor.innerHTML = items.map(i => `
            <div class="barra-fila">
                <span class="barra-fila__etiqueta">${escapeHtml(i.etiqueta)}</span>
                <div class="barra-fila__pista">
                    <div class="barra-fila__relleno" style="width: ${(i.valor / maximo * 100).toFixed(1)}%"></div>
                </div>
                <span class="barra-fila__valor">${formatearValor(i.valor)}</span>
            </div>`
        ).join('');
    }

    // ---------- Chips de filtro ----------

    function renderizarChips(contenedor, items, idProp, nombreProp, excluidos, onToggle) {
        contenedor.innerHTML = items.map(item => {
            const id = item[idProp];
            const activo = !excluidos.has(id);
            return `<button type="button" class="chip ${activo ? 'chip--activo' : ''}" data-id="${id}">${escapeHtml(item[nombreProp])}</button>`;
        }).join('');

        contenedor.querySelectorAll('.chip').forEach(chip => {
            chip.addEventListener('click', () => {
                const id = parseInt(chip.dataset.id, 10);
                if (excluidos.has(id)) excluidos.delete(id); else excluidos.add(id);
                chip.classList.toggle('chip--activo');
                onToggle();
            });
        });
    }

    // ---------- Carga de datos ----------

    function idsIncluidos(todos, excluidos, prop) {
        const incluidos = todos.filter(x => !excluidos.has(x[prop])).map(x => x[prop]);
        // Si están todos incluidos, no hace falta mandar el filtro (la API trae todo).
        return incluidos.length === todos.length ? null : incluidos;
    }

    async function cargarCanchas(fecha) {
        const ids = idsIncluidos(canchas, canchasExcluidas, 'cod_Cancha');
        const query = ids ? `?fecha=${fecha}&canchas=${ids.join(',')}` : `?fecha=${fecha}`;
        const url = `/Estadistica/${periodo === 'dia' ? 'Dia' : periodo === 'semana' ? 'Semana' : 'Mes'}Cancha${query}`;

        const respuesta = await apiFetch(url);
        if (!respuesta.ok) return null;
        return await respuesta.json();
    }

    async function cargarVentas(fecha) {
        const ids = idsIncluidos(productos, productosExcluidos, 'cod_Producto');
        const query = ids ? `?fecha=${fecha}&productos=${ids.join(',')}` : `?fecha=${fecha}`;
        const url = `/Estadistica/${periodo === 'dia' ? 'Dia' : periodo === 'semana' ? 'Semana' : 'Mes'}${query}`;

        const respuesta = await apiFetch(url);
        if (!respuesta.ok) return null;
        return await respuesta.json();
    }

    async function actualizarDashboard() {
        const fecha = inputFecha.value;
        if (!fecha) return;

        const [datosCancha, datosVenta] = await Promise.all([cargarCanchas(fecha), cargarVentas(fecha)]);

        // ---- KPIs ----
        kpiReservas.textContent = datosCancha ? datosCancha.totalReservas : '-';
        kpiIngresos.textContent = datosVenta ? formatearMoneda(datosVenta.totalMonto) : '-';
        kpiCantidadVendida.textContent = datosVenta ? datosVenta.totalCantidad : '-';

        // ---- Panel Canchas: siempre "total de reservas por cancha" ----
        if (datosCancha && datosCancha.filas.length > 0) {
            canchaVacio.hidden = true;
            const items = datosCancha.filas
                .map(f => ({ etiqueta: f.cancha, valor: f.total }))
                .sort((a, b) => b.valor - a.valor);
            renderizarBarras(barrasCancha, items, v => `${v} turno${v === 1 ? '' : 's'}`);
        } else {
            canchaVacio.hidden = false;
            barrasCancha.innerHTML = '';
        }

        // ---- Panel Ventas: por producto (día) o por día/semana (semana/mes) ----
        if (periodo === 'dia') {
            tituloPanelProducto.textContent = 'Ventas por producto';
            if (datosVenta && datosVenta.productos.length > 0) {
                productoVacio.hidden = true;
                const items = datosVenta.productos
                    .map(p => ({ etiqueta: p.nombre, valor: p.total }))
                    .sort((a, b) => b.valor - a.valor);
                renderizarBarras(barrasProducto, items, formatearMoneda);
            } else {
                productoVacio.hidden = false;
                barrasProducto.innerHTML = '';
            }
        } else {
            const puntos = periodo === 'semana' ? datosVenta?.dias : datosVenta?.semanas;
            tituloPanelProducto.textContent = periodo === 'semana' ? 'Ingresos por día' : 'Ingresos por semana';
            if (puntos && puntos.length > 0 && puntos.some(p => p.total > 0)) {
                productoVacio.hidden = true;
                const items = puntos.map(p => ({ etiqueta: p.etiqueta, valor: p.total }));
                renderizarBarras(barrasProducto, items, formatearMoneda);
            } else {
                productoVacio.hidden = false;
                barrasProducto.innerHTML = '';
            }
        }
    }

    // ---------- Eventos ----------

    botonesPeriodo.forEach(boton => {
        boton.addEventListener('click', () => {
            botonesPeriodo.forEach(b => b.classList.remove('is-active'));
            boton.classList.add('is-active');
            periodo = boton.dataset.periodo;
            actualizarDashboard();
        });
    });

    inputFecha.addEventListener('change', actualizarDashboard);

    // ---------- Inicio ----------

    // Fecha local (no toISOString, que es UTC y después de las 21 hs en Argentina daba el día siguiente).
    const hoy = new Date();
    inputFecha.value = `${hoy.getFullYear()}-${String(hoy.getMonth() + 1).padStart(2, '0')}-${String(hoy.getDate()).padStart(2, '0')}`;
    renderizarChips(chipsCanchas, canchas, 'cod_Cancha', 'nombre', canchasExcluidas, actualizarDashboard);
    renderizarChips(chipsProductos, productos, 'cod_Producto', 'nombre', productosExcluidos, actualizarDashboard);
    actualizarDashboard();
})();
