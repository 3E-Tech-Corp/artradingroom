const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5000';

async function request(method, path, body) {
  const opts = {
    method,
    headers: { 'Content-Type': 'application/json' },
  };
  if (body !== undefined) opts.body = JSON.stringify(body);
  const res = await fetch(`${API_BASE}${path}`, opts);
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`${res.status} ${res.statusText}: ${text}`);
  }
  const ct = res.headers.get('content-type') || '';
  if (ct.includes('application/json')) return res.json();
  return res.text();
}

// Trades
export const getTrades = () => request('GET', '/api/trades');
export const placeTrade = (trade) => request('POST', '/api/trades', trade);

// Strategies
export const getStrategies = () => request('GET', '/api/strategies');
export const createStrategy = (s) => request('POST', '/api/strategies', s);
export const deleteStrategy = (id) => request('DELETE', `/api/strategies/${id}`);
export const runBacktest = (id, params) => request('POST', `/api/strategies/${id}/backtest`, params);
export const getBacktests = (id) => request('GET', `/api/strategies/${id}/backtests`);

// Agents
export const getAgents = () => request('GET', '/api/agents');
export const createAgentTask = (id, task) => request('POST', `/api/agents/${id}/tasks`, task);

// Risk
export const getRiskThresholds = () => request('GET', '/api/risk/thresholds');
export const updateRiskThreshold = (id, data) => request('PUT', `/api/risk/thresholds/${id}`, data);

// Audit
export const getAuditLog = (params) => {
  const qs = params ? '?' + new URLSearchParams(params).toString() : '';
  return request('GET', `/api/audit${qs}`);
};

// Research
export const generateBrief = () => request('POST', '/api/research/brief');
export const scanMarket = (params) => request('POST', '/api/research/scan', params);

// Trading / Portfolio
export const getPortfolio = () => request('GET', '/api/trading/portfolio');
export const getPositions = () => request('GET', '/api/trading/positions');

// Health
export const getHealth = () => request('GET', '/health');
