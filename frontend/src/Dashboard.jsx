import React, { useEffect, useState } from 'react';
import { fetchDashboard } from './api';
import { 
  Users, 
  GraduationCap, 
  UserCheck, 
  Clock, 
  ArrowRight, 
  Database, 
  RefreshCw,
  AlertCircle 
} from 'lucide-react';

export default function Dashboard({ onNavigate, onViewStudent }) {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await fetchDashboard();
      setData(res);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  if (loading) {
    return (
      <div className="state-box">
        <div className="spinner"></div>
        <p style={{ marginTop: '12px' }}>Loading institutional dashboard...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="card">
        <div className="card-body state-box">
          <AlertCircle size={40} color="var(--danger)" className="state-box-icon" />
          <h3 className="state-box-title">Unable to Load Dashboard</h3>
          <p className="state-box-desc">{error}</p>
          <button className="btn btn-primary" onClick={loadData}>
            <RefreshCw size={14} /> Try Again
          </button>
        </div>
      </div>
    );
  }

  const totalStudents = Number(data?.totalStudents ?? 0);
  const activeStudents = Number(data?.activeStudents ?? 0);
  const admittedStudents = Number(data?.admittedStudents ?? 0);
  const programCount = Number(data?.programCount ?? 0);
  const isUsingMongoDb = Boolean(data?.isUsingMongoDb);
  const programBreakdown = Array.isArray(data?.programBreakdown) ? data.programBreakdown : [];
  const recentAdmissions = Array.isArray(data?.recentAdmissions) ? data.recentAdmissions : [];

  return (
    <div>
      <div className="page-header">
        <div>
          <h1 className="page-title">Executive Overview</h1>
          <p className="page-subtitle">Vidyadeep Institute of Technology — Academic Year 2026–2027 (Term 2026-ODD)</p>
        </div>
        <div style={{ display: 'flex', gap: '10px' }}>
          <button className="btn btn-outline" onClick={loadData}>
            <RefreshCw size={14} /> Refresh Metrics
          </button>
          <button className="btn btn-outline" onClick={() => onNavigate('registrations')}>
            Term Registration
          </button>
          <button className="btn btn-primary" onClick={() => onNavigate('add-student')}>
            Register New Student
          </button>
        </div>
      </div>

      {/* Persistence Info Banner */}
      <div style={{
        backgroundColor: isUsingMongoDb ? '#f0fdf4' : '#fffbeb',
        border: `1px solid ${isUsingMongoDb ? 'rgba(5, 150, 105, 0.25)' : 'rgba(217, 119, 6, 0.25)'}`,
        padding: '12px 18px',
        borderRadius: 'var(--radius-md)',
        marginBottom: '24px',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        flexWrap: 'wrap',
        gap: '12px'
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
          <Database size={18} color={isUsingMongoDb ? 'var(--success)' : 'var(--warning)'} />
          <div>
            <span style={{ fontSize: '13px', fontWeight: 600, color: isUsingMongoDb ? 'var(--success)' : 'var(--warning)' }}>
              Persistence Store: {isUsingMongoDb ? 'Local MongoDB Active (Port 27017)' : 'In-Memory Resilient Store'}
            </span>
            <p style={{ fontSize: '12px', color: 'var(--text-secondary)', margin: 0 }}>
              {isUsingMongoDb 
                ? 'All student records, admission sequences, and updates are persisted to the MongoDB instance.' 
                : 'MongoDB connection is offline. Operating in resilient in-memory mode.'}
            </p>
          </div>
        </div>
        <span className={`badge-pill ${isUsingMongoDb ? 'online' : 'warning'}`}>
          <span className="badge-dot"></span>
          {isUsingMongoDb ? 'MongoDB Connected' : 'In-Memory Active'}
        </span>
      </div>

      {/* Metric Cards */}
      <div className="metrics-grid">
        <div className="metric-card">
          <div className="metric-icon-box teal">
            <Users size={24} />
          </div>
          <div>
            <div className="metric-value">{totalStudents}</div>
            <div className="metric-label">Total Registered Students</div>
          </div>
        </div>

        <div className="metric-card">
          <div className="metric-icon-box emerald">
            <UserCheck size={24} />
          </div>
          <div>
            <div className="metric-value">{activeStudents}</div>
            <div className="metric-label">Active Enrolled Students</div>
          </div>
        </div>

        <div className="metric-card">
          <div className="metric-icon-box blue">
            <Clock size={24} />
          </div>
          <div>
            <div className="metric-value">{admittedStudents}</div>
            <div className="metric-label">Pending Term Registration</div>
          </div>
        </div>

        <div className="metric-card">
          <div className="metric-icon-box purple">
            <GraduationCap size={24} />
          </div>
          <div>
            <div className="metric-value">{programCount}</div>
            <div className="metric-label">Degree Programs Offered</div>
          </div>
        </div>
      </div>

      {/* Program Distribution */}
      <div className="card">
        <div className="card-header">
          <div>
            <h2 className="card-title">Program Enrollment Distribution</h2>
            <p style={{ fontSize: '12px', color: 'var(--text-muted)', margin: '2px 0 0 0' }}>
              Enrolled student headcount and active status breakdown across academic programs
            </p>
          </div>
          <button className="btn btn-outline btn-sm" onClick={() => onNavigate('programs')}>
            View All Programs <ArrowRight size={13} />
          </button>
        </div>
        <div className="card-body">
          {programBreakdown.length === 0 ? (
            <div style={{ textAlign: 'center', padding: '24px', color: 'var(--text-muted)' }}>
              No program enrollment data recorded yet.
            </div>
          ) : (
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '16px' }}>
              {programBreakdown.map((p, idx) => {
                const programName = p?.program || p?.Program || `Program ${idx + 1}`;
                const progTotal = Math.max(0, Number(p?.count ?? p?.Count ?? 0));
                const progActive = Math.max(0, Number(p?.activeCount ?? p?.ActiveCount ?? 0));
                const activePercent = progTotal > 0 ? Math.min(100, Math.round((progActive / progTotal) * 100)) : 0;
                const shareOfTotal = totalStudents > 0 ? Math.min(100, Math.round((progTotal / totalStudents) * 100)) : 0;

                return (
                  <div 
                    key={programName} 
                    style={{
                      backgroundColor: 'var(--surface-50)',
                      border: '1px solid var(--surface-200)',
                      borderRadius: 'var(--radius-sm)',
                      padding: '16px',
                      display: 'flex',
                      flexDirection: 'column',
                      justifyContent: 'space-between',
                      gap: '12px'
                    }}
                  >
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '8px' }}>
                      <span style={{ fontSize: '13.5px', fontWeight: 700, color: 'var(--primary-900)', wordBreak: 'break-word' }}>
                        {programName}
                      </span>
                      <div style={{ textAlign: 'right', flexShrink: 0 }}>
                        <span style={{ fontSize: '18px', fontWeight: 700, color: 'var(--accent-600)' }}>
                          {progTotal}
                        </span>
                        <span style={{ fontSize: '11px', color: 'var(--text-muted)', display: 'block', marginTop: '-2px' }}>
                          {shareOfTotal}% of total
                        </span>
                      </div>
                    </div>
                    <div>
                      <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '11.5px', color: 'var(--text-muted)', marginBottom: '5px' }}>
                        <span>Active Enrolled</span>
                        <span style={{ fontWeight: 600, color: 'var(--text-secondary)' }}>
                          {progActive} of {progTotal} ({activePercent}%)
                        </span>
                      </div>
                      <div style={{ width: '100%', height: '6px', backgroundColor: 'var(--surface-200)', borderRadius: '3px', overflow: 'hidden' }}>
                        <div 
                          style={{ 
                            width: `${activePercent}%`, 
                            height: '100%', 
                            backgroundColor: 'var(--accent-600)',
                            borderRadius: '3px',
                            transition: 'width 0.3s ease'
                          }} 
                        />
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>

      {/* Recent Admissions Table */}
      <div className="card">
        <div className="card-header">
          <h2 className="card-title">Recent Admissions</h2>
          <button className="btn btn-outline btn-sm" onClick={() => onNavigate('directory')}>
            Explore Directory <ArrowRight size={13} />
          </button>
        </div>
        <div className="table-wrapper">
          <table className="data-table">
            <thead>
              <tr>
                <th>Student</th>
                <th>Admission No</th>
                <th>Program</th>
                <th>Admission Date</th>
                <th>Status</th>
                <th style={{ textAlign: 'right' }}>Action</th>
              </tr>
            </thead>
            <tbody>
              {recentAdmissions.length === 0 ? (
                <tr>
                  <td colSpan={6} style={{ textAlign: 'center', padding: '30px' }}>
                    No admissions recorded yet.
                  </td>
                </tr>
              ) : (
                recentAdmissions.map((s) => {
                  const firstName = s.firstName || '';
                  const lastName = s.lastName || '';
                  const fullName = s.fullName || `${firstName} ${lastName}`.trim() || 'Unnamed Student';
                  const initials = `${firstName[0] || fullName[0] || 'S'}${lastName[0] || ''}`.toUpperCase();
                  const formattedDate = s.admissionDate
                    ? new Date(s.admissionDate).toLocaleDateString('en-IN', {
                        day: '2-digit',
                        month: 'short',
                        year: 'numeric'
                      })
                    : '—';
                  const status = s.lifecycleStatus || 'ADMITTED';

                  return (
                    <tr key={s.id || s.admissionNumber}>
                      <td>
                        <div className="student-cell">
                          <div className="student-avatar">{initials}</div>
                          <div>
                            <div className="student-name">{fullName}</div>
                            <div className="student-email">{s.email || '—'}</div>
                          </div>
                        </div>
                      </td>
                      <td>
                        <span style={{ fontFamily: 'monospace', fontWeight: 600, color: 'var(--primary-800)' }}>
                          {s.admissionNumber || '—'}
                        </span>
                      </td>
                      <td>
                        <span style={{ fontWeight: 500 }}>{s.program || '—'}</span>
                        <span style={{ fontSize: '11.5px', color: 'var(--text-muted)', marginLeft: '6px' }}>
                          (Sem {s.currentSemester ?? 1})
                        </span>
                      </td>
                      <td>{formattedDate}</td>
                      <td>
                        <span className={`status-tag ${status.toLowerCase()}`}>
                          {status}
                        </span>
                      </td>
                      <td style={{ textAlign: 'right' }}>
                        <button 
                          className="btn btn-outline btn-sm"
                          onClick={() => onViewStudent(s.id)}
                        >
                          View Details
                        </button>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
