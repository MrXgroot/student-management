import React, { useState, useEffect } from 'react';
import {
  fetchRegistrationOverview,
  fetchAcademicPeriods,
  fetchPrograms
} from './api';
import RegistrationModal from './RegistrationModal';
import {
  Search,
  Filter,
  CheckCircle,
  Clock,
  AlertCircle,
  Calendar,
  BookOpen,
  UserCheck,
  UserPlus,
  RefreshCw,
  GraduationCap,
  Layers,
  ChevronRight
} from 'lucide-react';

export default function TermRegistrationsView({ onNavigate, onViewStudent }) {
  const [overview, setOverview] = useState([]);
  const [periods, setPeriods] = useState([]);
  const [programs, setPrograms] = useState([]);
  
  // Filters
  const [selectedPeriod, setSelectedPeriod] = useState('2026-ODD');
  const [selectedProgram, setSelectedProgram] = useState('ALL');
  const [selectedSemester, setSelectedSemester] = useState(0);
  const [searchQuery, setSearchQuery] = useState('');
  
  // Modal state
  const [modalOpen, setModalOpen] = useState(false);
  const [modalStudent, setModalStudent] = useState(null);
  const [modalRegistrationId, setModalRegistrationId] = useState(null);

  // Loading & error
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    loadInitialData();
  }, []);

  useEffect(() => {
    if (selectedPeriod) {
      loadOverview();
    }
  }, [selectedPeriod, selectedProgram, selectedSemester, searchQuery]);

  const loadInitialData = async () => {
    try {
      const [periodsData, programsData] = await Promise.all([
        fetchAcademicPeriods(),
        fetchPrograms()
      ]);
      setPeriods(periodsData || []);
      setPrograms(programsData || []);
      
      const current = periodsData?.find(p => p.isCurrent);
      if (current) {
        setSelectedPeriod(current.termCode || current.code);
      }
    } catch (err) {
      console.warn('Initial data load error:', err);
    }
  };

  const loadOverview = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await fetchRegistrationOverview(
        selectedPeriod,
        selectedProgram,
        selectedSemester,
        searchQuery
      );
      setOverview(data || []);
    } catch (err) {
      setError(err.message || 'Failed to load term registration data');
    } finally {
      setLoading(false);
    }
  };

  const handleOpenRegisterModal = (item) => {
    const s = item.student || {};
    setModalStudent({
      id: item.studentId || s.id,
      admissionNumber: item.admissionNumber || s.admissionNumber,
      fullName: item.studentName || s.fullName,
      programCode: item.programCode || s.program,
      status: item.studentLifecycleStatus || s.lifecycleStatus,
      rollNumber: item.rollNumber || s.rollNumber
    });
    setModalRegistrationId(null);
    setModalOpen(true);
  };

  const handleOpenManageModal = (item) => {
    const s = item.student || {};
    const r = item.registration || {};
    setModalStudent({
      id: item.studentId || s.id,
      admissionNumber: item.admissionNumber || s.admissionNumber,
      fullName: item.studentName || s.fullName,
      programCode: item.programCode || s.program,
      status: item.studentLifecycleStatus || s.lifecycleStatus,
      rollNumber: item.rollNumber || s.rollNumber
    });
    setModalRegistrationId(item.termRegistrationId || r.id);
    setModalOpen(true);
  };

  const handleModalSaved = () => {
    loadOverview();
  };

  // Metrics
  const totalEligible = overview.length;
  const registeredCount = overview.filter(s => s.isRegistered || Boolean(s.registration?.id)).length;
  const pendingCount = totalEligible - registeredCount;
  const totalCreditsEnrolled = overview.reduce((sum, s) => sum + (s.totalEnrolledCredits || s.totalCredits || 0), 0);

  const activePeriodObj = periods.find(p => p.code === selectedPeriod);

  return (
    <div className="view-container">
      {/* Page Header */}
      <div className="page-header" style={{ marginBottom: '24px' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '4px' }}>
            <h1 className="page-title" style={{ margin: 0 }}>Term Registration & Enrollment</h1>
            <span className="badge badge-success" style={{ fontSize: '11px' }}>
              Active Term: {selectedPeriod}
            </span>
          </div>
          <p className="page-subtitle" style={{ margin: 0 }}>
            Manage student semester registration, enforce unique enrollment constraints, and allocate academic courses.
          </p>
        </div>

        <div style={{ display: 'flex', gap: '8px' }}>
          <button
            className="btn btn-outline btn-sm"
            onClick={loadOverview}
            disabled={loading}
            title="Refresh Registration Records"
          >
            <RefreshCw size={14} className={loading ? 'spin' : ''} />
            <span style={{ marginLeft: '4px' }}>Refresh</span>
          </button>
        </div>
      </div>

      {/* Metric Summary Cards */}
      <div className="stats-grid" style={{ marginBottom: '24px' }}>
        <div className="stat-card">
          <div className="stat-label">Eligible Students</div>
          <div className="stat-value">{totalEligible}</div>
          <div className="stat-desc">Students matching filter</div>
        </div>

        <div className="stat-card">
          <div className="stat-label">Term Registered</div>
          <div className="stat-value" style={{ color: 'var(--success)' }}>{registeredCount}</div>
          <div className="stat-desc">
            {totalEligible > 0 ? Math.round((registeredCount / totalEligible) * 100) : 0}% Registered for {selectedPeriod}
          </div>
        </div>

        <div className="stat-card">
          <div className="stat-label">Pending Registration</div>
          <div className="stat-value" style={{ color: pendingCount > 0 ? 'var(--warning)' : 'var(--text-muted)' }}>
            {pendingCount}
          </div>
          <div className="stat-desc">Awaiting term registration</div>
        </div>

        <div className="stat-card">
          <div className="stat-label">Total Course Credits</div>
          <div className="stat-value" style={{ color: 'var(--accent-600)' }}>
            {totalCreditsEnrolled}
          </div>
          <div className="stat-desc">Credits enrolled this term</div>
        </div>
      </div>

      {/* Filter and Search Bar */}
      <div className="card" style={{ padding: '16px', marginBottom: '20px' }}>
        <div style={{ display: 'flex', gap: '12px', flexWrap: 'wrap', alignItems: 'center' }}>
          
          {/* Period Selector */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
            <Calendar size={15} color="var(--accent-600)" />
            <select
              className="form-select"
              value={selectedPeriod}
              onChange={(e) => setSelectedPeriod(e.target.value)}
              style={{ fontWeight: 600, minWidth: '150px' }}
            >
              {periods.map(p => {
                const code = p.termCode || p.code;
                const name = p.termName || p.name;
                return (
                  <option key={code} value={code}>
                    {name} ({code}) {p.isCurrent ? '• Active' : ''}
                  </option>
                );
              })}
            </select>
          </div>

          {/* Program Filter */}
          <div style={{ minWidth: '160px' }}>
            <select
              className="form-select"
              value={selectedProgram}
              onChange={(e) => setSelectedProgram(e.target.value)}
            >
              <option value="ALL">All Degree Programs</option>
              {programs.map(p => (
                <option key={p.code} value={p.code}>
                  {p.code} ({p.name})
                </option>
              ))}
            </select>
          </div>

          {/* Semester Filter */}
          <div style={{ minWidth: '140px' }}>
            <select
              className="form-select"
              value={selectedSemester}
              onChange={(e) => setSelectedSemester(parseInt(e.target.value, 10))}
            >
              <option value={0}>All Semesters</option>
              <option value={1}>Semester 1 (Odd)</option>
              <option value={2}>Semester 2 (Even)</option>
              <option value={3}>Semester 3 (Odd)</option>
              <option value={4}>Semester 4 (Even)</option>
              <option value={5}>Semester 5 (Odd)</option>
              <option value={6}>Semester 6 (Even)</option>
            </select>
          </div>

          {/* Search Bar */}
          <div style={{ flex: 1, minWidth: '220px', position: 'relative' }}>
            <Search size={15} style={{ position: 'absolute', left: '10px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }} />
            <input
              type="text"
              className="form-control"
              placeholder="Search by student name or admission number..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              style={{ paddingLeft: '32px' }}
            />
          </div>

          {(selectedProgram !== 'ALL' || selectedSemester !== 0 || searchQuery) && (
            <button
              className="btn btn-outline btn-sm"
              onClick={() => {
                setSelectedProgram('ALL');
                setSelectedSemester(0);
                setSearchQuery('');
              }}
              style={{ fontSize: '12px' }}
            >
              Clear Filters
            </button>
          )}

        </div>
      </div>

      {/* Error state */}
      {error && (
        <div style={{
          padding: '16px',
          background: '#fef2f2',
          border: '1px solid #fecaca',
          borderRadius: 'var(--radius-md)',
          color: '#b91c1c',
          marginBottom: '20px',
          display: 'flex',
          alignItems: 'center',
          gap: '10px'
        }}>
          <AlertCircle size={18} />
          <span>{error}</span>
        </div>
      )}

      {/* Table Section */}
      <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '13px' }}>
          <thead>
            <tr style={{ background: 'var(--surface-50)', borderBottom: '1px solid var(--surface-200)' }}>
              <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)' }}>Student</th>
              <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)' }}>Program</th>
              <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)' }}>Lifecycle</th>
              <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)' }}>Term Status</th>
              <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)' }}>Enrolled Courses</th>
              <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)', textAlign: 'right' }}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={6} style={{ padding: '36px', textAlign: 'center', color: 'var(--text-muted)' }}>
                  <div style={{ display: 'inline-flex', alignItems: 'center', gap: '8px' }}>
                    <RefreshCw size={16} className="spin" />
                    <span>Loading student registration records...</span>
                  </div>
                </td>
              </tr>
            ) : overview.length === 0 ? (
              <tr>
                <td colSpan={6} style={{ padding: '36px', textAlign: 'center', color: 'var(--text-muted)' }}>
                  No students found matching the selected criteria.
                </td>
              </tr>
            ) : (
              overview.map((item) => {
                const s = item.student || {};
                const r = item.registration || {};
                const studentId = item.studentId || s.id;
                const studentName = item.studentName || s.fullName || 'Unnamed Student';
                const admissionNumber = item.admissionNumber || s.admissionNumber || '—';
                const rollNumber = item.rollNumber || s.rollNumber;
                const programCode = item.programCode || s.program || '—';
                const lifecycleStatus = item.studentLifecycleStatus || s.lifecycleStatus || 'ADMITTED';
                const isRegistered = Boolean(item.isRegistered || r.id);
                const regStatus = item.registrationStatus || r.status || 'REGISTERED';
                const regSemester = item.registeredSemester ?? r.semester;
                const periodCode = item.academicPeriodCode || r.academicPeriodCode || selectedPeriod;
                const courseCount = item.enrolledCourseCount ?? (r.enrolledCourses?.length || 0);
                const totalCredits = item.totalEnrolledCredits ?? item.totalCredits ?? 0;

                return (
                  <tr key={studentId} style={{ borderBottom: '1px solid var(--surface-200)', transition: 'background 0.15s' }}>
                    
                    {/* Student Details */}
                    <td style={{ padding: '12px 16px' }}>
                      <div style={{ fontWeight: 600, color: 'var(--text-primary)', fontSize: '14px' }}>
                        {studentName}
                      </div>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '6px', marginTop: '2px' }}>
                        <span style={{ fontSize: '11px', color: 'var(--text-muted)', fontFamily: 'monospace' }}>
                          {admissionNumber}
                        </span>
                        {rollNumber && (
                          <span style={{ fontSize: '11px', background: '#e0f2fe', color: '#0369a1', padding: '1px 6px', borderRadius: '4px', fontWeight: 600 }}>
                            Roll: {rollNumber}
                          </span>
                        )}
                      </div>
                    </td>

                    {/* Program */}
                    <td style={{ padding: '12px 16px' }}>
                      <span className="badge badge-outline" style={{ fontWeight: 600 }}>
                        {programCode}
                      </span>
                    </td>

                    {/* Student Lifecycle */}
                    <td style={{ padding: '12px 16px' }}>
                      <span className={`badge ${lifecycleStatus === 'ACTIVE' ? 'badge-success' : 'badge-info'}`}>
                        {lifecycleStatus}
                      </span>
                    </td>

                    {/* Term Registration Status */}
                    <td style={{ padding: '12px 16px' }}>
                      {isRegistered ? (
                        <div>
                          <span className={`badge ${regStatus === 'CONFIRMED' ? 'badge-success' : 'badge-warning'}`} style={{ fontWeight: 600 }}>
                            {regStatus}
                          </span>
                          <div style={{ fontSize: '11px', color: 'var(--text-muted)', marginTop: '2px' }}>
                            Sem {regSemester} • {periodCode}
                          </div>
                        </div>
                      ) : (
                        <span className="badge badge-outline" style={{ color: 'var(--text-muted)', borderColor: '#cbd5e1' }}>
                          Not Registered
                        </span>
                      )}
                    </td>

                    {/* Enrolled Courses */}
                    <td style={{ padding: '12px 16px' }}>
                      {isRegistered ? (
                        <div>
                          <div style={{ fontWeight: 600, color: courseCount > 0 ? 'var(--text-primary)' : 'var(--warning)' }}>
                            {courseCount} course(s)
                          </div>
                          <div style={{ fontSize: '11px', color: 'var(--text-muted)' }}>
                            {totalCredits} Total Credits
                          </div>
                        </div>
                      ) : (
                        <span style={{ color: 'var(--text-muted)', fontSize: '12px' }}>—</span>
                      )}
                    </td>

                    {/* Actions */}
                    <td style={{ padding: '12px 16px', textAlign: 'right' }}>
                      {isRegistered ? (
                        <button
                          type="button"
                          className="btn btn-outline btn-sm"
                          onClick={() => handleOpenManageModal(item)}
                          style={{ display: 'inline-flex', alignItems: 'center', gap: '4px' }}
                        >
                          <BookOpen size={13} />
                          <span>Manage Courses</span>
                        </button>
                      ) : (
                        <button
                          type="button"
                          className="btn btn-primary btn-sm"
                          onClick={() => handleOpenRegisterModal(item)}
                          style={{ display: 'inline-flex', alignItems: 'center', gap: '4px' }}
                        >
                          <UserPlus size={13} />
                          <span>Register for Term</span>
                        </button>
                      )}
                    </td>

                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>

      {/* Registration & Course Modal */}
      {modalOpen && (
        <RegistrationModal
          student={modalStudent}
          registrationId={modalRegistrationId}
          activePeriodCode={selectedPeriod}
          onClose={() => setModalOpen(false)}
          onSaved={handleModalSaved}
        />
      )}
    </div>
  );
}
