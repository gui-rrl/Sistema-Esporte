const API = '';

// Aplica o tema salvo antes de qualquer renderização
(function() {
  const t = localStorage.getItem('se_tema') || 'escuro';
  document.documentElement.setAttribute('data-tema', t);
})();

// ===== SweetAlert2 — carrega dinamicamente se ainda não estiver presente =====
(function() {
  if (document.getElementById('sa2-css')) return;
  const css = document.createElement('link');
  css.id = 'sa2-css'; css.rel = 'stylesheet';
  css.href = 'https://cdn.jsdelivr.net/npm/sweetalert2@11/dist/sweetalert2.min.css';
  document.head.appendChild(css);
  const js = document.createElement('script');
  js.src = 'https://cdn.jsdelivr.net/npm/sweetalert2@11';
  document.head.appendChild(js);
})();

function _sa2() { return window.Swal; }

function _swalOpts() {
  const claro = document.documentElement.getAttribute('data-tema') === 'claro';
  return {
    background: claro ? '#ffffff' : '#1a1a2e',
    color:      claro ? '#0d1f10' : '#e2e8f0',
  };
}

/** Substitui alert() */
function sAlert(msg, titulo = '', tipo = 'info') {
  return new Promise(res => {
    const run = () => _sa2().fire({
      title: titulo || undefined,
      text:  msg,
      icon:  tipo,
      confirmButtonText: 'OK',
      confirmButtonColor: '#16a34a',
      ..._swalOpts(),
    }).then(() => res());
    _sa2() ? run() : setTimeout(() => _sa2() ? run() : window.alert(msg), 600);
  });
}

/** Substitui confirm() — retorna Promise<boolean> */
function sConfirm(msg, titulo = 'Confirmar', tipo = 'warning') {
  return new Promise(res => {
    const run = () => _sa2().fire({
      title: titulo,
      text:  msg,
      icon:  tipo,
      showCancelButton:  true,
      confirmButtonText: 'Confirmar',
      cancelButtonText:  'Cancelar',
      confirmButtonColor: '#16a34a',
      cancelButtonColor:  '#374151',
      ..._swalOpts(),
    }).then(r => res(r.isConfirmed));
    _sa2() ? run() : setTimeout(() => _sa2() ? run() : res(window.confirm(msg)), 600);
  });
}

function getToken()   { return localStorage.getItem('se_token'); }
function getUsuario() { return localStorage.getItem('se_usuario'); }
function getPapel()   { return localStorage.getItem('se_papel'); }
function getTimeId()  { return localStorage.getItem('se_timeId'); }
function isAdmin()    { return getPapel() === 'Admin'; }
function isLogado()   { return !!getToken(); }

function salvarSessao(data) {
  localStorage.setItem('se_token',   data.token);
  localStorage.setItem('se_usuario', data.nomeUsuario);
  localStorage.setItem('se_papel',   data.papel);
  localStorage.setItem('se_timeId',  data.timeId ?? '');
}

function limparSessao() {
  ['se_token','se_usuario','se_papel','se_timeId'].forEach(k => localStorage.removeItem(k));
}

function logout() { limparSessao(); window.location.href = '/login.html'; }

function exigirLogin() {
  if (!isLogado()) { window.location.href = '/login.html'; return false; }
  return true;
}

function toggleTema() {
  const atual = document.documentElement.getAttribute('data-tema') || 'escuro';
  const novo  = atual === 'escuro' ? 'claro' : 'escuro';
  document.documentElement.setAttribute('data-tema', novo);
  localStorage.setItem('se_tema', novo);
  const btn = document.getElementById('btnTema');
  if (btn) btn.textContent = novo === 'escuro' ? '☀️' : '🌙';
}

async function apiFetch(url, opcoes = {}) {
  const token = getToken();
  const headers = { 'Content-Type': 'application/json', ...(opcoes.headers ?? {}) };
  if (token) headers['Authorization'] = `Bearer ${token}`;
  const resp = await fetch(API + url, { ...opcoes, headers });
  if (resp.status === 401) { logout(); return null; }
  return resp;
}

function renderNavbar(container) {
  if (!container) return;
  const usuario = getUsuario();
  const papel   = getPapel();
  const pag     = location.pathname;
  const claro   = document.documentElement.getAttribute('data-tema') === 'claro';

  const navLink = (href, label, icon) => {
    const active = pag === href || pag.startsWith(href.replace('.html','')) ? 'active' : '';
    return `<a href="${href}" class="nav-link ${active}">${icon}<span class="nav-label"> ${label}</span></a>`;
  };

  container.innerHTML = `
    <a href="/index.html" class="navbar-brand">
      <span class="logo">⚽</span>
      <span class="brand-text">Sistema Esporte</span>
    </a>
    <nav class="nav-pills">
      ${navLink('/index.html',    'Times',    '👥')}
      ${navLink('/partidas.html', 'Partidas', '📋')}
      ${navLink('/torneios.html', 'Torneios', '🏆')}
      ${navLink('/peladas.html',  'Peladas',  '⚽')}
      ${usuario ? navLink('/quero-jogar.html', 'Quero Jogar', '🙋') : ''}
      ${papel === 'Admin' ? navLink('/admin-solicitacoes.html', 'Solicitações', '📥') : ''}
    </nav>
    <div class="navbar-end">
      <button onclick="toggleTema()" id="btnTema" class="btn btn-secundario btn-sm" title="Alternar tema" style="font-size:1rem;padding:.35rem .55rem">${claro ? '🌙' : '☀️'}</button>
      ${usuario
        ? `<div class="user-chip">
             <span class="avatar-xs">${usuario[0].toUpperCase()}</span>
             <span class="user-name">${usuario}</span>
             <span class="role-tag">${papel}</span>
           </div>
           <button onclick="logout()" class="btn btn-secundario btn-sm"><span class="nav-label">Sair</span><span class="nav-icon-only">✕</span></button>`
        : `<a href="/login.html" class="btn btn-primario btn-sm">Entrar</a>`
      }
    </div>`;
}
