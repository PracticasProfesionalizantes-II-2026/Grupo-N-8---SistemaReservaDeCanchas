(() => {
    let ventas = Array.isArray(ventasIniciales) ? ventasIniciales : [];
    const productos = Array.isArray(productosIniciales) ? productosIniciales : [];

    let itemsVenta = [];      // { cod_Producto, nombre, cantidad, precio }
    let idParaEliminar = null;

    // ---------- Elementos ----------

    const listaEl = document.getElementById('listaVentas');
    const inputBuscar = document.getElementById('inputBuscar');

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

        const filtradas = ventas.filter(v => {
            if (!texto) return true;
            const etiqueta = `venta #${v.cod_Venta} ${formatearFecha(v.fecha)} ${formatearHora(v.hora)} ${v.nombre_Usuario ?? ''}`.toLowerCase();
            return etiqueta.includes(texto);
        });

        if (filtradas.length === 0) {
            listaEl.innerHTML = '<div class="productos-vacio">No se encontraron ventas.</div>';
            return;
        }

        listaEl.innerHTML = filtradas.map(v => `
            <div class="venta-card" data-id="${v.cod_Venta}">
                <div class="venta-card__info">
                    <span class="venta-card__titulo">Venta #${v.cod_Venta}</span>
                    <span class="venta-card__fecha">${formatearFecha(v.fecha)} ${formatearHora(v.hora)}</span>
                </div>
                <button type="button" class="btn-eliminar" data-accion="eliminar" data-id="${v.cod_Venta}" title="Eliminar">✕</button>
            </div>`
        ).join('');
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
            const respuesta = await fetch('/Venta/Create', {
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

        agregarOActualizarItem(cod_Producto, cantidad);
        cerrarModalAgregarProducto();
    }

    // ---------- Ver Detalles ----------

    async function abrirModalDetalles(id) {
        try {
            const respuesta = await fetch(`/Venta/Details/${id}`);
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
            const respuesta = await fetch(`/Venta/Delete/${idParaEliminar}`, { method: 'POST' });
            const datos = await respuesta.json();

            if (!respuesta.ok) {
                bajaError.textContent = datos.mensaje || 'No se pudo eliminar la venta';
                return;
            }

            ventas = ventas.filter(v => v.cod_Venta !== idParaEliminar);
            cerrarModalBaja();
            renderizarVentas();
            mostrarToast('Venta eliminada');
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

        const tarjeta = evento.target.closest('.venta-card');
        if (tarjeta) {
            abrirModalDetalles(parseInt(tarjeta.dataset.id, 10));
        }
    });

    inputBuscar.addEventListener('input', renderizarVentas);

    [modalNuevaVenta, modalAgregarProducto, modalDetalles, modalBaja].forEach(overlay => {
        overlay.addEventListener('click', (evento) => {
            if (evento.target === overlay) overlay.hidden = true;
        });
    });

    renderizarVentas();
})();
