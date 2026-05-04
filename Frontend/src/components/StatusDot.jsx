const STATUS_COLORS = {
  idle: '#888',
  working: '#00ff88',
  error: '#ff4444',
  active: '#00ff88',
  draft: '#888',
  backtested: '#4488ff',
  paper_trading: '#ffaa00',
  live: '#00ff88',
  archived: '#555',
};

export default function StatusDot({ status, size = 10 }) {
  const color = STATUS_COLORS[status] || '#888';
  return (
    <span
      style={{
        display: 'inline-block',
        width: size,
        height: size,
        borderRadius: '50%',
        background: color,
        boxShadow: `0 0 6px ${color}`,
        flexShrink: 0,
      }}
      title={status}
    />
  );
}
