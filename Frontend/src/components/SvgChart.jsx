export default function SvgChart({ data = [], width = 400, height = 120, color = '#00ff88', label = '' }) {
  if (!data || data.length < 2) {
    return (
      <svg width={width} height={height} style={{ display: 'block' }}>
        <rect width={width} height={height} fill="#111" rx="4" />
        <text x={width / 2} y={height / 2 + 5} textAnchor="middle" fill="#555" fontSize="13" fontFamily="monospace">
          No chart data
        </text>
      </svg>
    );
  }

  const pad = { top: 8, right: 8, bottom: 20, left: 40 };
  const cw = width - pad.left - pad.right;
  const ch = height - pad.top - pad.bottom;

  const min = Math.min(...data);
  const max = Math.max(...data);
  const range = max - min || 1;

  const points = data.map((v, i) => {
    const x = pad.left + (i / (data.length - 1)) * cw;
    const y = pad.top + ch - ((v - min) / range) * ch;
    return [x, y];
  });

  const d = points.map(([x, y], i) => `${i === 0 ? 'M' : 'L'}${x.toFixed(1)},${y.toFixed(1)}`).join(' ');
  const fillD = `${d} L${points[points.length - 1][0].toFixed(1)},${(pad.top + ch).toFixed(1)} L${pad.left.toFixed(1)},${(pad.top + ch).toFixed(1)} Z`;

  const fmtVal = (v) => {
    if (Math.abs(v) >= 1000) return `${(v / 1000).toFixed(1)}k`;
    return v.toFixed(0);
  };

  return (
    <svg width={width} height={height} style={{ display: 'block' }}>
      <rect width={width} height={height} fill="#0a0f0a" rx="4" />

      {/* Grid lines */}
      {[0, 0.25, 0.5, 0.75, 1].map((t) => {
        const y = pad.top + t * ch;
        const v = max - t * range;
        return (
          <g key={t}>
            <line x1={pad.left} y1={y} x2={pad.left + cw} y2={y} stroke="#1a2a1a" strokeWidth="1" />
            <text x={pad.left - 4} y={y + 4} textAnchor="end" fill="#446644" fontSize="9" fontFamily="monospace">
              {fmtVal(v)}
            </text>
          </g>
        );
      })}

      {/* Area fill */}
      <defs>
        <linearGradient id={`grad-${label}`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={color} stopOpacity="0.3" />
          <stop offset="100%" stopColor={color} stopOpacity="0.02" />
        </linearGradient>
      </defs>
      <path d={fillD} fill={`url(#grad-${label})`} />

      {/* Line */}
      <path d={d} fill="none" stroke={color} strokeWidth="1.5" />

      {/* Last value dot */}
      <circle
        cx={points[points.length - 1][0]}
        cy={points[points.length - 1][1]}
        r="3"
        fill={color}
        stroke="#0a0f0a"
        strokeWidth="1"
      />
    </svg>
  );
}
