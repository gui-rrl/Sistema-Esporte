const API = '';

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
