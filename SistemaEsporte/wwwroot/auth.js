const API = '';

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

/** Substitui alert() */
function sAlert(msg, titulo = '', tipo = 'info') {
  return new Promise(res => {
    const run = () => _sa2().fire({
      title: titulo || undefined,
      text:  msg,
      icon:  tipo,
      confirmButtonText: 'OK',
      confirmButtonColor: '#16a34a',
      background: '#1a1a2e',
      color: '#e2e8f0',
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
      background: '#1a1a2e',
      color: '#e2e8f0',
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

  const navLink = (href, label, icon) => {
    const active = pag === href || pag.startsWith(href.replace('.html','')) ? 'active' : '';
    return `<a href="${href}" class="nav-link ${active}">${icon} ${label}</a>`;
  };

  container.innerHTML = `
    <a href="/index.html" class="navbar-brand">
      <span class="logo">⚽</span>
      Sistema Esporte
    </a>
    <nav class="nav-pills">
      ${navLink('/index.html',    'Ranking',  '🏅')}
      ${navLink('/partidas.html', 'Partidas', '📋')}
      ${navLink('/torneios.html', 'Torneios', '🏆')}
    </nav>
    <div class="navbar-end">
      ${usuario
        ? `<div class="user-chip">
             <span class="avatar-xs">${usuario[0].toUpperCase()}</span>
             <span>${usuario}</span>
             <span class="role-tag">${papel}</span>
           </div>
           <button onclick="logout()" class="btn btn-secundario btn-sm">Sair</button>`
        : `<a href="/login.html" class="btn btn-primario btn-sm">Entrar</a>`
      }
    </div>`;
}
