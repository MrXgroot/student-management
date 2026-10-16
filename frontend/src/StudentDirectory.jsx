import React, { useEffect, useState } from 'react';
import { fetchStudents } from './api';
import { Search, Filter, Eye, Edit3, UserPlus, RefreshCw, AlertCircle } from 'lucide-react';

const PROGRAMS = [
  'ALL',
  'BCA',
  'B.Sc. Computer Science',
  'B.Com.',
  'BBA',
  'MCA',
  'MBA'
];

const STATUSES = [
  'ALL',
  'ADMITTED',
  'ACTIVE',
  'PROBATION',
  'ON_LEAVE',
  'WITHDRAWN'
];

export default function StudentDirectory({ initialProgramFilter = 'ALL', onNavigate, onViewStudent, onEditStudent }) {
  const [students, setStudents] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [search, setSearch] = useState('');
  const [programFilter, setProgramFilter] = useState(initialProgramFilter);
  const [statusFilter, setStatusFilter] = useState('ALL');

  const loadStudents = async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await fetchStudents(search, statusFilter, programFilter);
      setStudents(res);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    const timer = setTimeout(() => {
      loadStudents();
    }, 200);
    return () => clearTimeout(timer);
  }, [search, statusFilter, programFilter]);

  const handleClearFilters = () => {
    setSearch('');
    setProgramFilter('ALL');
    setStatusFilter('ALL');
  };

  return (
    <div>
      <div className="page-header">
        <div>
          <h1 className="page-title">Student Directory</h1>
          <p className="page-subtitle">Search, filter, and review enrolled and admitted student profiles</p>
        </div>
        <div style={{ display: 'flex', gap: '10px' }}>
          <button className="btn btn-outline" onClick={loadStudents}>
            <RefreshCw size={14} /> Refresh
          </button>
          <button className="btn btn-primary" onClick={() => onNavigate('add-student')}>
            <UserPlus size={15} /> Add New Student
          </button>
        </div>
      </div>

      {/* Filter and Search Bar */}
      <div className="card">
        <div className="card-body" style={{ padding: '16px 20px' }}>
          <div className="filter-bar">
            <div className="search-input-wrapper">
              <Search size={16} />
              <input
                type="text"
                className="search-input"
                placeholder="Search by name, admission no, roll no, or email..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
            </div>

            <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
              <Filter size={15} color="var(--text-muted)" />
              <select
                className="select-filter"
                value={programFilter}
                onChange={(e) => setProgramFilter(e.target.value)}
              >
                {PROGRAMS.map((p) => (
                  <option key={p} value={p}>
                    {p === 'ALL' ? 'All Programs' : p}
                  </option>
                ))}
              </select>

              <select
                className="select-filter"
                value={statusFilter}
                onChange={(e) => setStatusFilter(e.target.value)}
              >
                {STATUSES.map((s) => (
                  <option key={s} value={s}>
                    {s === 'ALL' ? 'All Statuses' : s}
                  </option>
                ))}
              </select>

              {(search || programFilter !== 'ALL' || statusFilter !== 'ALL') && (
                <button className="btn btn-outline btn-sm" onClick={handleClearFilters}>
                  Clear
                </button>
              )}
            </div>
          </div>

          <div style={{ fontSize: '12px', color: 'var(--text-muted)', display: 'flex', justifyContent: 'space-between' }}>
            <span>Showing {students.length} student records</span>
            {(search || programFilter !== 'ALL' || statusFilter !== 'ALL') && (
              <span>Filtered results</span>
            )}
          </div>
        </div>

        {/* Table Content */}
        {loading ? (
          <div className="state-box">
            <div className="spinner"></div>
            <p style={{ marginTop: '12px' }}>Loading student records...</p>
          </div>
        ) : error ? (
          <div className="state-box">
            <AlertCircle size={36} color="var(--danger)" className="state-box-icon" />
            <h3 className="state-box-title">Error Loading Students</h3>
            <p className="state-box-desc">{error}</p>
            <button className="btn btn-primary btn-sm" onClick={loadStudents}>Retry</button>
          </div>
        ) : students.length === 0 ? (
          <div className="state-box">
            <Search size={36} className="state-box-icon" />
            <h3 className="state-box-title">No Students Found</h3>
            <p className="state-box-desc">
              No student records matched your search query or filter criteria.
            </p>
            <button className="btn btn-outline btn-sm" onClick={handleClearFilters}>
              Clear Search Filters
            </button>
          </div>
        ) : (
          <div className="table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Student Details</th>
                  <th>Admission Number</th>
                  <th>Roll Number</th>
                  <th>Program & Semester</th>
                  <th>Contact</th>
                  <th>Lifecycle Status</th>
                  <th style={{ textAlign: 'right' }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {students.map((s) => {
                  const initials = `${s.firstName?.[0] || ''}${s.lastName?.[0] || ''}`;
                  return (
                    <tr key={s.id}>
                      <td>
                        <div className="student-cell">
                          <div className="student-avatar">{initials}</div>
                          <div>
                            <div className="student-name">{s.fullName}</div>
                            <div className="student-email">{s.email}</div>
                          </div>
                        </div>
                      </td>
                      <td>
                        <span style={{ fontFamily: 'monospace', fontWeight: 600, color: 'var(--primary-800)' }}>
                          {s.admissionNumber}
                        </span>
                      </td>
                      <td>
                        {s.rollNumber ? (
                          <span style={{ fontFamily: 'monospace', fontSize: '13px', fontWeight: 600, color: 'var(--text-secondary)' }}>
                            {s.rollNumber}
                          </span>
                        ) : (
                          <span style={{ fontSize: '11px', color: 'var(--text-muted)', fontStyle: 'italic' }}>
                            Unassigned
                          </span>
                        )}
                      </td>
                      <td>
                        <div style={{ fontWeight: 600 }}>{s.program}</div>
                        <div style={{ fontSize: '12px', color: 'var(--text-muted)' }}>Semester {s.currentSemester}</div>
                      </td>
                      <td>
                        <div style={{ fontSize: '13px' }}>{s.phone}</div>
                        <div style={{ fontSize: '11.5px', color: 'var(--text-muted)' }}>{s.address?.split(',')?.[0] || 'Bengaluru'}</div>
                      </td>
                      <td>
                        <span className={`status-tag ${s.lifecycleStatus?.toLowerCase()}`}>
                          {s.lifecycleStatus}
                        </span>
                      </td>
                      <td style={{ textAlign: 'right' }}>
                        <div style={{ display: 'inline-flex', gap: '6px' }}>
                          <button
                            className="btn btn-outline btn-sm"
                            title="View Student Profile"
                            onClick={() => onViewStudent(s.id)}
                          >
                            <Eye size={13} /> View
                          </button>
                          <button
                            className="btn btn-secondary btn-sm"
                            title="Edit Student Record"
                            onClick={() => onEditStudent(s.id)}
                          >
                            <Edit3 size={13} /> Edit
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
