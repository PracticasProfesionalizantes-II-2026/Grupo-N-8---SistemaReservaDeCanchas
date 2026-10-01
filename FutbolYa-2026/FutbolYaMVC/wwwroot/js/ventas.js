// Módulo de la página de ventas: los datos iniciales llegan serializados desde la vista Razor
// (ventasIniciales, productosIniciales); este script arma el carrito, renderiza el listado y
// llama a los endpoints de VentaController vía apiFetch.
(() => {
    let ventas = Array.isArray(ventasIniciales) ? ventasIniciales : [];
    // Solo se pueden elegir productos activos para una venta nueva; uno dado de baja
    // no debe aparecer como opción aunque tenga stock.
    const productos = (Array.isArray(productosIniciales) ? productosIniciales : []).filter(p => p.activo);

    let itemsVenta = [];      // { cod_Producto, nombre, cantidad, precio }
    let idParaEliminar = null;

    // ---------- Elementos ----------

    const listaEl = document.getElementById('listaVentas');
    const inputBuscar = document.getElementById('inputBuscar');
    const selectFiltroEstado = document.getElementById('selectFiltroEstado');
    const inputFiltroFecha = document.getElementById('inputFiltroFecha');

    const modalNuevaVenta = document.getElementById('modalNuevaVenta');
    const cuerpoTablaVenta = document.getElementById('cuerpoTablaVenta');
    const totalCantidadEl = document.getElementById('totalCantidad');
    const totalMontoEl = document.getElementById('totalMonto');
    const ventaError = document.getElementById('ventaError');

    const modalAgregarProducto = document.getElementById('modalAgregarProducto');
    const selectProducto = document.getElementById('selectProducto');
    const campoCantidadItem = document.getElementById('campoCantidadItem');
    const agregarProductoError = document.getElementById('agregarProductoError');

    const modalDetalles = document.getElementById('modalDetalles');
    const detallesTitulo = document.getElementById('detallesTitulo');
    const cuerpoTablaDetalles = document.getElementById('cuerpoTablaDetalles');
    const detallesTotalCantidad = document.getElementById('detallesTotalCantidad');
    const detallesTotalMonto = document.getElementById('detallesTotalMonto');

    const modalBaja = document.getElementById('modalBaja');
    const bajaError = document.getElementById('bajaError');

    const toast = document.getElementById('toast');

    // ---------- Utilidades ----------

    function mostrarToast(mensaje, esError = false) {
        toast.textContent = mensaje;
        toast.classList.toggle('toast--error', esError);
        toast.classList.add('toast--visible');
        clearTimeout(mostrarToast._t);
        mostrarToast._t = setTimeout(() => toast.classList.remove('toast--visible'), 3000);
    }

    function formatearPrecio(valor) {
        return new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', minimumFractionDigits: 0 })
            .format(valor)
            .replace('ARS', '$')
            .replace(/\s/g, '');
    }

    function escapeHtml(texto) {
        const div = document.createElement('div');
        div.textContent = texto;
        return div.innerHTML;
    }

    function formatearFecha(fechaIso) {
        const f = new Date(fechaIso);
        if (isNaN(f.getTime())) return fechaIso;
        const dia = String(f.getUTCDate()).padStart(2, '0');
        const mes = String(f.getUTCMonth() + 1).padStart(2, '0');
        const anio = f.getUTCFullYear();
        return `${dia}/${mes}/${anio}`;
    }

    function formatearHora(horaStr) {
        // La API serializa TimeSpan como "HH:mm:ss" (con posibles decimales al final).
        if (!horaStr) return '';
        return horaStr.substring(0, 5);
    }

    // ---------- Render lista de ventas ----------

    function renderizarVentas() {
        const texto = inputBuscar.value.trim().toLowerCase();
        const estado = selectFiltroEstado.value; // '', 'activo', 'baja'
        const fechaFiltro = inputFiltroFecha.value; // '' o 'yyyy-MM-dd'

        const filtradas = ventas.filter(v => {
            const coincideTexto = !texto || `venta #${v.cod_Venta} ${formatearFecha(v.fecha)} ${formatearHora(v.hora)} ${v.nombre_Usuario ?? ''}`.toLowerCase().includes(texto);
            const coincideEstado = !estado || (estado === 'activo' ? v.activo : !v.activo);
            const coincideFecha = !fechaFiltro || (v.fecha || '').substring(0, 10) === fechaFiltro;
            return coincideTexto && coincideEstado && coincideFecha;
        });

        if (filtradas.length === 0) {
            listaEl.innerHTML = '<div class="productos-vacio">No se encontraron ventas.</div>';
            return;
        }

        listaEl.innerHTML = filtradas.map(v => {
            const accion = v.activo
                ? `<button type="button" class="btn-eliminar" data-accion="eliminar" data-id="${v.cod_Venta}" title="Anular">✕</button>`
                : `<button type="button" class="btn-reactivar" data-accion="reactivar" data-id="${v.cod_Venta}">↺ Reactivar</button>`;

            return `
            <div class="venta-card ${v.activo ? '' : 'venta-card--baja'}" data-id="${v.cod_Venta}">
                <div class="venta-card__info">
                    <span class="venta-card__titulo">Venta #${v.cod_Venta} ${v.activo ? '' : '<span class="badge-baja">Anulada</span>'}</span>
                    <span class="venta-card__fecha">${formatearFecha(v.fecha)} ${formatearHora(v.hora)}</span>
                </div>
                ${accion}
            </div>`;
        }).join('');
    }

    // ---------- Nueva venta: tabla de items ----------

    function poblarSelectProductos() {
        selectProducto.innerHTML = productos
            .map(p => `<option value="${p.cod_Producto}">${escapeHtml(p.nombre)} (Stock: ${p.cantidad})</option>`)
            .join('');
    }

    function renderizarTablaVenta() {
        cuerpoTablaVenta.innerHTML = itemsVenta.map(item => {
            const subtotal = item.precio * item.cantidad;
            return `
                <tr data-cod-producto="${item.cod_Producto}">
                    <td>${escapeHtml(item.nombre)}</td>
                    <td>${item.cantidad}</td>
                    <td>${formatearPrecio(item.precio)}</td>
                    <td>${formatearPrecio(subtotal)}</td>
                    <td class="venta-tabla__acciones">
                        <button type="button" class="btn-quitar-item" data-cod-producto="${item.cod_Producto}" title="Quitar">✕</button>
                    </td>
                </tr>`;
        }).join('');

        const totalCantidad = itemsVenta.reduce((acum, i) => acum + i.cantidad, 0);
        const totalMonto = itemsVenta.reduce((acum, i) => acum + (i.precio * i.cantidad), 0);

        totalCantidadEl.textContent = totalCantidad;
        totalMontoEl.textContent = formatearPrecio(totalMonto);
    }

    function abrirModalNuevaVenta() {
        itemsVenta = [];
        ventaError.textContent = '';
        renderizarTablaVenta();
        modalNuevaVenta.hidden = false;
    }

    function cerrarModalNuevaVenta() {
        modalNuevaVenta.hidden = true;
    }

    function agregarOActualizarItem(cod_Producto, cantidad) {
        const producto = productos.find(p => p.cod_Producto === cod_Producto);
        if (!producto) return;

        const existente = itemsVenta.find(i => i.cod_Producto === cod_Producto);
        if (existente) {
            existente.cantidad += cantidad;
        } else {
            itemsVenta.push({
                cod_Producto,
                nombre: producto.nombre,
                precio: producto.precio,
                cantidad
            });
        }

        renderizarTablaVenta();
    }

    // Aplica en el array local `productos` la diferencia de stock que ya se reflejó en la API
    // (signo -1 al vender/reactivar, +1 al anular).
    function ajustarStockProductos(detalle, signo) {
        (detalle || []).forEach(d => {
            const producto = productos.find(p => p.cod_Producto === d.cod_Producto);
            if (producto) producto.cantidad += signo * d.cantidad;
        });
    }

    async function confirmarVenta() {
        ventaError.textContent = '';

        if (itemsVenta.length === 0) {
            ventaError.textContent = 'Agregá al menos un producto a la venta';
            return;
        }

        const request = {
            detalle: itemsVenta.map(i => ({ cod_Producto: i.cod_Producto, cantidad: i.cantidad }))
        };

        try {
            const respuesta = await apiFetch('/Venta/Create', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(request)
            });

            const datos = await respuesta.json();

            if (!respuesta.ok) {
                ventaError.textContent = datos.mensaje || 'No se pudo registrar la venta';
                return;
            }

            ventas.push(datos);
            // Refleja en el selector de productos el stock recién descontado.
            ajustarStockProductos(datos.detalle, -1);
            cerrarModalNuevaVenta();
            renderizarVentas();
            mostrarToast('Venta registrada correctamente');
        } catch (ex) {
            ventaError.textContent = 'Error de conexión con el servidor';
        }
    }

    // ---------- Submodal Agregar Producto ----------

    function abrirModalAgregarProducto() {
        agregarProductoError.textContent = '';
        poblarSelectProductos();
        campoCantidadItem.value = 1;
        modalAgregarProducto.hidden = false;
    }

    function cerrarModalAgregarProducto() {
        modalAgregarProducto.hidden = true;
    }

    function confirmarAgregarProducto() {
        agregarProductoError.textContent = '';

        const cod_Producto = parseInt(selectProducto.value, 10);
        const cantidad = parseInt(campoCantidadItem.value, 10);

        if (!cod_Producto) {
            agregarProductoError.textContent = 'Seleccioná un producto';
            return;
        }

        if (!cantidad || cantidad <= 0) {
            agregarProductoError.textContent = 'La cantidad debe ser mayor a 0';
            return;
        }

        // La suma de lo ya cargado en el carrito más lo nuevo no puede superar el stock actual.
        const producto = productos.find(p => p.cod_Producto === cod_Producto);
        const yaEnCarrito = itemsVenta.find(i => i.cod_Producto === cod_Producto)?.cantidad ?? 0;

        if (producto && (yaEnCarrito + cantidad) > producto.cantidad) {
            agregarProductoError.textContent = `Stock insuficiente para "${producto.nombre}". Disponible: ${producto.cantidad}${yaEnCarrito > 0 ? ` (ya agregaste ${yaEnCarrito})` : ''}`;
            return;
        }

        agregarOActualizarItem(cod_Producto, cantidad);
        ventaError.textContent = '';
        cerrarModalAgregarProducto();
    }

    // ---------- Ver Detalles ----------

    async function abrirModalDetalles(id) {
        try {
            const respuesta = await apiFetch(`/Venta/Details/${id}`);
            if (!respuesta.ok) {
                mostrarToast('No se pudo obtener el detalle de la venta', true);
                return;
            }

            const venta = await respuesta.json();
            detallesTitulo.textContent = `Venta #${venta.cod_Venta}`;

            const detalle = venta.detalle || [];
            cuerpoTablaDetalles.innerHTML = detalle.map(d => `
                <tr>
                    <td>${escapeHtml(d.nombre_Producto)}</td>
                    <td>${d.cantidad}</td>
                    <td>${formatearPrecio(d.precio)}</td>
                    <td>${formatearPrecio(d.subTotal)}</td>
                </tr>`
            ).join('');

            const totalCantidad = detalle.reduce((acum, d) => acum + d.cantidad, 0);
            detallesTotalCantidad.textContent = totalCantidad;
            detallesTotalMonto.textContent = formatearPrecio(venta.montoTotal);

            modalDetalles.hidden = false;
        } catch (ex) {
            mostrarToast('Error de conexión con el servidor', true);
        }
    }

    function cerrarModalDetalles() {
        modalDetalles.hidden = true;
    }

    // ---------- Reactivar ----------

    async function reactivarVenta(id) {
        try {
            const respuesta = await apiFetch(`/Venta/Reactivar/${id}`, { method: 'POST' });
            const datos = await respuesta.json();

            if (!respuesta.ok) {
                mostrarToast(datos.mensaje || 'No se pudo reactivar la venta', true);
                return;
            }

            ventas = ventas.map(v => v.cod_Venta === datos.cod_Venta ? datos : v);
            // Reactivar vuelve a descontar stock.
            ajustarStockProductos(datos.detalle, -1);
            renderizarVentas();
            mostrarToast('Venta reactivada correctamente');
        } catch (ex) {
            mostrarToast('Error de conexión con el servidor', true);
        }
    }

    // ---------- Modal Baja ----------

    function abrirModalBaja(id) {
        idParaEliminar = id;
        bajaError.textContent = '';
        modalBaja.hidden = false;
    }

    function cerrarModalBaja() {
        modalBaja.hidden = true;
        idParaEliminar = null;
    }

    async function confirmarBaja() {
        if (idParaEliminar == null) return;
        bajaError.textContent = '';

        try {
            const respuesta = await apiFetch(`/Venta/Delete/${idParaEliminar}`, { method: 'POST' });
            const datos = await respuesta.json();

            if (!respuesta.ok) {
                bajaError.textContent = datos.mensaje || 'No se pudo eliminar la venta';
                return;
            }

            // La venta se mantiene en la lista como Anulada (con botón Reactivar) en vez de
            // desaparecer; el detalle viene en la respuesta para reponer el stock localmente.
            if (datos.venta) {
                ventas = ventas.map(v => v.cod_Venta === idParaEliminar ? datos.venta : v);
                ajustarStockProductos(datos.venta.detalle, +1);
            } else {
                ventas = ventas.map(v => v.cod_Venta === idParaEliminar ? { ...v, activo: false } : v);
            }
            cerrarModalBaja();
            renderizarVentas();
            mostrarToast('Venta anulada');
        } catch (ex) {
            bajaError.textContent = 'Error de conexión con el servidor';
        }
    }

    // ---------- Eventos ----------

    document.getElementById('btnAbrirVenta').addEventListener('click', abrirModalNuevaVenta);
    document.getElementById('btnCancelarVenta').addEventListener('click', cerrarModalNuevaVenta);
    document.getElementById('btnConfirmarVenta').addEventListener('click', confirmarVenta);

    document.getElementById('btnAbrirAgregarProducto').addEventListener('click', abrirModalAgregarProducto);
    document.getElementById('btnCancelarAgregarProducto').addEventListener('click', cerrarModalAgregarProducto);
    document.getElementById('btnConfirmarAgregarProducto').addEventListener('click', confirmarAgregarProducto);

    document.getElementById('btnVolverDetalles').addEventListener('click', cerrarModalDetalles);

    document.getElementById('btnCancelarBaja').addEventListener('click', cerrarModalBaja);
    document.getElementById('btnConfirmarBaja').addEventListener('click', confirmarBaja);

    cuerpoTablaVenta.addEventListener('click', (evento) => {
        const boton = evento.target.closest('.btn-quitar-item');
        if (!boton) return;
        ventaError.textContent = '';
        const codProducto = parseInt(boton.dataset.codProducto, 10);
        itemsVenta = itemsVenta.filter(i => i.cod_Producto !== codProducto);
        renderizarTablaVenta();
    });

    listaEl.addEventListener('click', (evento) => {
        const botonEliminar = evento.target.closest('button[data-accion="eliminar"]');
        if (botonEliminar) {
            abrirModalBaja(parseInt(botonEliminar.dataset.id, 10));
            return;
        }

        const botonReactivar = evento.target.closest('button[data-accion="reactivar"]');
        if (botonReactivar) {
            reactivarVenta(parseInt(botonReactivar.dataset.id, 10));
            return;
        }

        const tarjeta = evento.target.closest('.venta-card');
        if (tarjeta) {
            abrirModalDetalles(parseInt(tarjeta.dataset.id, 10));
        }
    });

    inputBuscar.addEventListener('input', renderizarVentas);
    selectFiltroEstado.addEventListener('change', renderizarVentas);
    inputFiltroFecha.addEventListener('change', renderizarVentas);

    [modalNuevaVenta, modalAgregarProducto, modalDetalles, modalBaja].forEach(overlay => {
        overlay.addEventListener('click', (evento) => {
            if (evento.target === overlay) overlay.hidden = true;
        });
    });

    renderizarVentas();
})();
