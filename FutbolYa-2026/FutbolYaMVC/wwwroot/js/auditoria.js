// Módulo de la página de auditoría: los datos iniciales llegan serializados desde la vista
// Razor (auditoriasIniciales, usuariosParaFiltro); este script filtra el historial por DNI y
// muestra el detalle (valores anterior/nuevo) de cada entrada.
(() => {
    const auditorias = Array.isArray(auditoriasIniciales) ? auditoriasIniciales : [];
    const usuarios = Array.isArray(usuariosParaFiltro) ? usuariosParaFiltro : [];

    const cuerpoTabla = document.getElementById('cuerpoTablaAuditoria');
    const inputBuscarDni = document.getElementById('inputBuscarDni');

    const modalDetalle = document.getElementById('modalDetalleAuditoria');
    const cuerpoDetalle = document.getElementById('cuerpoDetalleAuditoria');

    function escapeHtml(texto) {
        const div = document.createElement('div');
        div.textContent = texto ?? '';
        return div.innerHTML;
    }

    function formatearFechaHora(fechaIso) {
        const f = new Date(fechaIso);
        if (isNaN(f.getTime())) return fechaIso;
        const dia = String(f.getDate()).padStart(2, '0');
        const mes = String(f.getMonth() + 1).padStart(2, '0');
        const anio = f.getFullYear();
        const hora = String(f.getHours()).padStart(2, '0');
        const min = String(f.getMinutes()).padStart(2, '0');
        return `${dia}/${mes}/${anio} ${hora}:${min}`;
    }

    function formatearJson(texto) {
        if (!texto) return null;
        try {
            return JSON.stringify(JSON.parse(texto), null, 2);
        } catch {
            return texto;
        }
    }

    // Se filtra por DNI (no repudio) en vez de por Cod_Usuario, resolviendo el DNI
    // ingresado a los usuarios que coinciden entre los ya cargados en el módulo.
    function codsUsuarioQueCoincidenConDni(textoDni) {
        return new Set(
            usuarios
                .filter(u => u.dni.includes(textoDni))
                .map(u => u.cod_Usuario)
        );
    }

    function renderizar() {
        const textoDni = inputBuscarDni.value.trim();

        const filtradas = textoDni
            ? auditorias.filter(a => codsUsuarioQueCoincidenConDni(textoDni).has(a.cod_Usuario))
            : auditorias;

        if (filtradas.length === 0) {
            cuerpoTabla.innerHTML = '<tr><td colspan="5">No se encontraron registros.</td></tr>';
            return;
        }

        cuerpoTabla.innerHTML = filtradas.map((a, indice) => {
            const claseAccion = `auditoria-accion--${a.accion.toLowerCase()}`;
            const tieneDetalle = !!(a.valor_Anterior || a.valor_Nuevo);

            return `
            <tr>
                <td>${formatearFechaHora(a.fecha_Hora)}</td>
                <td>${escapeHtml(a.nombre_Usuario)}</td>
                <td><span class="auditoria-accion ${claseAccion}">${escapeHtml(a.accion)}</span></td>
                <td>${escapeHtml(a.entidad_Afectada)} #${a.cod_Entidad_Afectada}</td>
                <td>
                    ${tieneDetalle ? `<button type="button" class="btn-ver-detalle" data-indice="${indice}">Ver detalle</button>` : '—'}
                </td>
            </tr>`;
        }).join('');

        cuerpoTabla.dataset.filtradas = JSON.stringify(filtradas.map(a => ({
            valor_Anterior: a.valor_Anterior,
            valor_Nuevo: a.valor_Nuevo
        })));
    }

    function abrirDetalle(indice) {
        const filtradas = JSON.parse(cuerpoTabla.dataset.filtradas || '[]');
        const item = filtradas[indice];
        if (!item) return;

        const anterior = formatearJson(item.valor_Anterior);
        const nuevo = formatearJson(item.valor_Nuevo);

        cuerpoDetalle.innerHTML = `
            ${anterior ? `<div class="auditoria-detalle-bloque"><strong>Antes</strong><pre>${escapeHtml(anterior)}</pre></div>` : ''}
            ${nuevo ? `<div class="auditoria-detalle-bloque"><strong>${anterior ? 'Después' : 'Valor'}</strong><pre>${escapeHtml(nuevo)}</pre></div>` : ''}
        `;
        modalDetalle.hidden = false;
    }

    function cerrarDetalle() {
        modalDetalle.hidden = true;
    }

    cuerpoTabla.addEventListener('click', (evento) => {
        const boton = evento.target.closest('.btn-ver-detalle');
        if (!boton) return;
        abrirDetalle(parseInt(boton.dataset.indice, 10));
    });

    document.getElementById('btnCerrarDetalleAuditoria').addEventListener('click', cerrarDetalle);
    modalDetalle.addEventListener('click', (evento) => {
        if (evento.target === modalDetalle) cerrarDetalle();
    });

    inputBuscarDni.addEventListener('input', renderizar);

    renderizar();
})();
