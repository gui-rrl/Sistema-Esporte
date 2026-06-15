// Utilitários de autenticação compartilhados
const API = '';

function getToken()    { return localStorage.getItem('se_token'); }
function getUsuario()  { return localStorage.getItem('se_usuario'); }
function getPapel()    { return localStorage.getItem('se_papel'); }
function getTimeId()   { return localStorage.getItem('se_timeId'); }
function isAdmin()     { return getPapel() === 'Admin'; }
function isLogado()    { return !!getToken(); }

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

function exigirAdmin() {
  if (!exigirLogin()) return false;
  if (!isAdmin()) { window.location.href = '/index.html'; return false; }
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
  container.innerHTML = `
    <span class="navbar-brand">⚽ Sistema Esporte</span>
    <nav class="navbar-actions">
      <a href="/index.html" class="btn btn-secundario btn-sm">Ranking</a>
      <a href="/partidas.html" class="btn btn-secundario btn-sm">Partidas</a>
      <a href="/torneios.html" class="btn btn-secundario btn-sm">Torneios</a>
      ${usuario ? `<span class="nav-user"><span class="nav-badge">${papel}</span> ${usuario}</span>` : ''}
      ${usuario
        ? `<button onclick="logout()" class="btn btn-secundario btn-sm">Sair</button>`
        : `<a href="/login.html" class="btn btn-primario btn-sm">Entrar</a>`}
    </nav>`;
}
