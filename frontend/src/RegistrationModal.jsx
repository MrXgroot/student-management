import React, { useState, useEffect } from 'react';
import {
  fetchRegistrationById,
  registerStudentForTerm,
  updateRegistrationStatus,
  fetchEligibleCourses,
  enrollCourseInRegistration,
  dropCourseFromRegistration,
  fetchAcademicPeriods
} from './api';
import {
  X,
  BookOpen,
  Plus,
  Trash2,
  CheckCircle,
  AlertCircle,
  Clock,
  Calendar,
  GraduationCap,
  ShieldCheck,
  UserCheck
} from 'lucide-react';

export default function RegistrationModal({
  student,              // { id, admissionNumber, fullName, programCode, status, rollNumber }
  registrationId = null,// If already registered, pass registrationId
  activePeriodCode = '2026-ODD',
  onClose,
  onSaved
}) {
  const [activeTab, setActiveTab] = useState(registrationId ? 'courses' : 'register');
  const [regId, setRegId] = useState(registrationId);
  const [regDetail, setRegDetail] = useState(null);
  const [periods, setPeriods] = useState([]);
  const [eligibleCourses, setEligibleCourses] = useState([]);
  const [selectedCourseId, setSelectedCourseId] = useState('');
  
  // Form fields for new registration
  const [selectedPeriod, setSelectedPeriod] = useState(activePeriodCode || '2026-ODD');
  const [selectedSemester, setSelectedSemester] = useState(1);
  
  // Loading & error states
  const [loading, setLoading] = useState(false);
  const [enrolling, setEnrolling] = useState(false);
  const [droppingId, setDroppingId] = useState(null);
  const [error, setError] = useState(null);
  const [successMsg, setSuccessMsg] = useState(null);

  useEffect(() => {
    loadPeriods();
    if (registrationId) {
      loadRegistrationDetail(registrationId);
    }
  }, [registrationId]);

  const loadPeriods = async () => {
    try {
      const data = await fetchAcademicPeriods();
      setPeriods(data || []);
      if (!selectedPeriod && data && data.length > 0) {
        const active = data.find(p => p.isCurrent) || data[0];
        setSelectedPeriod(active.termCode || active.code);
      }
    } catch (err) {
      console.warn('Failed to load periods:', err);
    }
  };

  const loadRegistrationDetail = async (id) => {
    try {
      setLoading(true);
      setError(null);
      const detail = await fetchRegistrationById(id);
      setRegDetail(detail);
      setRegId(detail.id);
      setActiveTab('courses');
      
      // Also fetch eligible courses for enrollment
      const eligible = await fetchEligibleCourses(id);
      setEligibleCourses(eligible || []);
      if (eligible && eligible.length > 0) {
        setSelectedCourseId(eligible[0].id);
      } else {
        setSelectedCourseId('');
      }
    } catch (err) {
      setError(err.message || 'Failed to load registration details');
    } finally {
      setLoading(false);
    }
  };

  const handleRegisterSubmit = async (e) => {
    e.preventDefault();
    try {
      setLoading(true);
      setError(null);
      setSuccessMsg(null);

      const created = await registerStudentForTerm({
        studentId: student.id,
        academicPeriodCode: selectedPeriod,
        semester: parseInt(selectedSemester, 10)
      });

      setSuccessMsg(`Student registered for ${created.academicPeriodCode} (Semester ${created.semester}) successfully!`);
      setRegId(created.id);
      
      // Load details and move to course management
      await loadRegistrationDetail(created.id);
      if (onSaved) onSaved(created);
    } catch (err) {
      setError(err.message || 'Registration failed');
    } finally {
      setLoading(false);
    }
  };

  const handleStatusChange = async (newStatus) => {
    if (!regId) return;
    try {
      setLoading(true);
      setError(null);
      const updated = await updateRegistrationStatus(regId, newStatus);
      setSuccessMsg(`Status updated to ${newStatus}`);
      await loadRegistrationDetail(regId);
      if (onSaved) onSaved(updated);
    } catch (err) {
      setError(err.message || 'Failed to update status');
    } finally {
      setLoading(false);
    }
  };

  const handleEnrollCourse = async (e) => {
    e.preventDefault();
    if (!regId || !selectedCourseId) return;

    try {
      setEnrolling(true);
      setError(null);
      setSuccessMsg(null);
      await enrollCourseInRegistration(regId, selectedCourseId);
      setSuccessMsg('Course enrolled successfully!');
      
      // Reload registration detail & remaining eligible courses
      await loadRegistrationDetail(regId);
      if (onSaved) onSaved(regDetail);
    } catch (err) {
      setError(err.message || 'Failed to enroll course');
    } finally {
      setEnrolling(false);
    }
  };

  const handleDropCourse = async (courseId, courseCode) => {
    if (!window.confirm(`Drop course ${courseCode}? This will remove the student's enrollment.`)) {
      return;
    }
    try {
      setDroppingId(courseId);
      setError(null);
      await dropCourseFromRegistration(regId, courseId);
      setSuccessMsg(`Course ${courseCode} dropped.`);
      await loadRegistrationDetail(regId);
      if (onSaved) onSaved(regDetail);
    } catch (err) {
      setError(err.message || 'Failed to drop course');
    } finally {
      setDroppingId(null);
    }
  };

  const totalCredits = regDetail?.enrolledCourses
    ? regDetail.enrolledCourses.reduce((sum, c) => sum + (c.credits || 0), 0)
    : 0;

  return (
    <div className="modal-backdrop">
      <div className="modal-container" style={{ maxWidth: '780px', width: '92%' }}>
        {/* Header */}
        <div className="modal-header">
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <div style={{
              width: '36px',
              height: '36px',
              borderRadius: '8px',
              background: 'var(--accent-50)',
              color: 'var(--accent-600)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center'
            }}>
              <GraduationCap size={20} />
            </div>
            <div>
              <h2 style={{ fontSize: '18px', fontWeight: 700, margin: 0 }}>
                {regId ? 'Term Registration & Course Enrollment' : 'Register Student for Academic Term'}
              </h2>
              <div style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
                {student?.fullName} ({student?.admissionNumber}) • {student?.programCode}
              </div>
            </div>
          </div>
          <button 
            onClick={onClose} 
            className="btn btn-outline btn-sm"
            style={{ padding: '6px', borderRadius: '50%' }}
          >
            <X size={16} />
          </button>
        </div>

        {/* Notifications */}
        {error && (
          <div style={{
            margin: '16px 24px 0 24px',
            padding: '12px 16px',
            backgroundColor: '#fef2f2',
            border: '1px solid #fecaca',
            borderRadius: 'var(--radius-sm)',
            color: '#b91c1c',
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
            fontSize: '13px'
          }}>
            <AlertCircle size={16} style={{ flexShrink: 0 }} />
            <span>{error}</span>
          </div>
        )}

        {successMsg && (
          <div style={{
            margin: '16px 24px 0 24px',
            padding: '12px 16px',
            backgroundColor: '#ecfdf5',
            border: '1px solid #a7f3d0',
            borderRadius: 'var(--radius-sm)',
            color: '#065f46',
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
            fontSize: '13px'
          }}>
            <CheckCircle size={16} style={{ flexShrink: 0 }} />
            <span>{successMsg}</span>
          </div>
        )}

        {/* Modal Body */}
        <div className="modal-body" style={{ padding: '20px 24px' }}>
          
          {/* Mode 1: New Term Registration Form */}
          {!regId && (
            <form onSubmit={handleRegisterSubmit}>
              <div style={{
                background: 'var(--surface-50)',
                border: '1px solid var(--surface-200)',
                borderRadius: 'var(--radius-md)',
                padding: '16px',
                marginBottom: '20px'
              }}>
                <div style={{ fontSize: '13px', fontWeight: 600, color: 'var(--text-secondary)', marginBottom: '8px' }}>
                  Student Summary
                </div>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: '12px', fontSize: '13px' }}>
                  <div>
                    <span style={{ color: 'var(--text-muted)' }}>Name:</span>{' '}
                    <strong>{student?.fullName}</strong>
                  </div>
                  <div>
                    <span style={{ color: 'var(--text-muted)' }}>Admission No:</span>{' '}
                    <code>{student?.admissionNumber}</code>
                  </div>
                  <div>
                    <span style={{ color: 'var(--text-muted)' }}>Program:</span>{' '}
                    <strong>{student?.programCode}</strong>
                  </div>
                  <div>
                    <span style={{ color: 'var(--text-muted)' }}>Lifecycle Status:</span>{' '}
                    <span className={`badge ${student?.status === 'ACTIVE' ? 'badge-success' : 'badge-info'}`}>
                      {student?.status}
                    </span>
                  </div>
                </div>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px', marginBottom: '20px' }}>
                <div className="form-group">
                  <label className="form-label" style={{ fontWeight: 600 }}>Academic Period *</label>
                  <select
                    className="form-select"
                    value={selectedPeriod}
                    onChange={(e) => setSelectedPeriod(e.target.value)}
                    required
                  >
                    {periods.map((p) => {
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

                <div className="form-group">
                  <label className="form-label" style={{ fontWeight: 600 }}>Semester *</label>
                  <select
                    className="form-select"
                    value={selectedSemester}
                    onChange={(e) => setSelectedSemester(parseInt(e.target.value, 10))}
                    required
                  >
                    <option value={1}>Semester 1 (Odd)</option>
                    <option value={2}>Semester 2 (Even)</option>
                    <option value={3}>Semester 3 (Odd)</option>
                    <option value={4}>Semester 4 (Even)</option>
                    <option value={5}>Semester 5 (Odd)</option>
                    <option value={6}>Semester 6 (Even)</option>
                  </select>
                </div>
              </div>

              {/* Informative Note */}
              <div style={{
                background: 'var(--info-bg)',
                border: '1px solid #bae6fd',
                borderRadius: 'var(--radius-sm)',
                padding: '12px 16px',
                fontSize: '12px',
                color: '#0369a1',
                marginBottom: '24px',
                lineHeight: 1.5
              }}>
                <div style={{ fontWeight: 600, display: 'flex', alignItems: 'center', gap: '6px', marginBottom: '4px' }}>
                  <ShieldCheck size={14} /> Institutional Policy & Invariants
                </div>
                Term registration records are unique per student, period, and semester.
                {student?.status === 'ADMITTED' && (
                  <span> Completing registration transitions student status to <strong>ACTIVE</strong> and assigns a permanent Roll Number.</span>
                )}
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '12px' }}>
                <button type="button" className="btn btn-outline" onClick={onClose} disabled={loading}>
                  Cancel
                </button>
                <button type="submit" className="btn btn-primary" disabled={loading}>
                  {loading ? 'Registering...' : 'Register for Term'}
                </button>
              </div>
            </form>
          )}

          {/* Mode 2: Existing Registration & Course Enrollments */}
          {regId && (
            <div>
              {/* Registration Banner */}
              <div style={{
                background: 'var(--surface-50)',
                border: '1px solid var(--surface-200)',
                borderRadius: 'var(--radius-md)',
                padding: '16px',
                marginBottom: '20px',
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                flexWrap: 'wrap',
                gap: '12px'
              }}>
                <div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '4px' }}>
                    <span style={{ fontWeight: 700, fontSize: '15px' }}>
                      {regDetail?.studentName || student?.fullName}
                    </span>
                    <span className="badge badge-outline" style={{ fontSize: '11px' }}>
                      {regDetail?.admissionNumber || student?.admissionNumber}
                    </span>
                  </div>
                  <div style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>
                    <span>{regDetail?.programCode || student?.programCode}</span> •{' '}
                    <span>Semester {regDetail?.semester}</span> •{' '}
                    <span>{regDetail?.academicPeriodCode}</span>
                  </div>
                </div>

                <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                  <div style={{ textAlign: 'right' }}>
                    <div style={{ fontSize: '11px', color: 'var(--text-muted)' }}>Status</div>
                    <select
                      className="form-select"
                      style={{ fontSize: '12px', padding: '4px 8px', fontWeight: 600 }}
                      value={regDetail?.status || 'REGISTERED'}
                      onChange={(e) => handleStatusChange(e.target.value)}
                      disabled={loading}
                    >
                      <option value="REGISTERED">REGISTERED</option>
                      <option value="CONFIRMED">CONFIRMED</option>
                      <option value="WITHDRAWN">WITHDRAWN</option>
                    </select>
                  </div>
                </div>
              </div>

              {/* Course Management Header & Stats */}
              <div style={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                marginBottom: '12px'
              }}>
                <div>
                  <h3 style={{ fontSize: '15px', fontWeight: 700, margin: 0 }}>
                    Enrolled Courses
                  </h3>
                  <div style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
                    {regDetail?.enrolledCourses?.length || 0} course(s) enrolled • Total {totalCredits} Credits
                  </div>
                </div>

                <span className={`badge ${totalCredits >= 16 ? 'badge-success' : 'badge-warning'}`} style={{ fontSize: '12px' }}>
                  {totalCredits} Credits Total
                </span>
              </div>

              {/* Enrolled Courses Table */}
              <div style={{
                border: '1px solid var(--surface-200)',
                borderRadius: 'var(--radius-md)',
                overflow: 'hidden',
                marginBottom: '24px'
              }}>
                <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '13px' }}>
                  <thead>
                    <tr style={{ background: 'var(--surface-50)', borderBottom: '1px solid var(--surface-200)', textAlign: 'left' }}>
                      <th style={{ padding: '10px 14px', fontWeight: 600, color: 'var(--text-secondary)' }}>Course Code</th>
                      <th style={{ padding: '10px 14px', fontWeight: 600, color: 'var(--text-secondary)' }}>Title</th>
                      <th style={{ padding: '10px 14px', fontWeight: 600, color: 'var(--text-secondary)' }}>Type</th>
                      <th style={{ padding: '10px 14px', fontWeight: 600, color: 'var(--text-secondary)' }}>Credits</th>
                      <th style={{ padding: '10px 14px', fontWeight: 600, color: 'var(--text-secondary)', textAlign: 'right' }}>Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {regDetail?.enrolledCourses && regDetail.enrolledCourses.length > 0 ? (
                      regDetail.enrolledCourses.map((c) => (
                        <tr key={c.id || c.courseId} style={{ borderBottom: '1px solid var(--surface-200)' }}>
                          <td style={{ padding: '10px 14px', fontWeight: 600, color: 'var(--accent-600)' }}>
                            {c.courseCode}
                          </td>
                          <td style={{ padding: '10px 14px', fontWeight: 500 }}>
                            {c.courseTitle}
                          </td>
                          <td style={{ padding: '10px 14px' }}>
                            <span className="badge badge-outline" style={{ fontSize: '11px' }}>
                              {c.courseType}
                            </span>
                          </td>
                          <td style={{ padding: '10px 14px', fontWeight: 600 }}>
                            {c.credits} Cr
                          </td>
                          <td style={{ padding: '10px 14px', textAlign: 'right' }}>
                            <button
                              type="button"
                              className="btn btn-outline btn-sm"
                              style={{ color: 'var(--danger)', borderColor: '#fecaca', padding: '4px 8px' }}
                              onClick={() => handleDropCourse(c.courseId, c.courseCode)}
                              disabled={droppingId === c.courseId}
                              title="Drop course"
                            >
                              <Trash2 size={13} />
                              <span style={{ marginLeft: '4px' }}>
                                {droppingId === c.courseId ? 'Dropping...' : 'Drop'}
                              </span>
                            </button>
                          </td>
                        </tr>
                      ))
                    ) : (
                      <tr>
                        <td colSpan={5} style={{ padding: '24px', textAlign: 'center', color: 'var(--text-muted)' }}>
                          No courses enrolled yet for this term. Add an eligible course below.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>

              {/* Add Course Enrollment Section */}
              <div style={{
                background: 'var(--surface-50)',
                border: '1px solid var(--surface-200)',
                borderRadius: 'var(--radius-md)',
                padding: '16px'
              }}>
                <div style={{ fontSize: '13px', fontWeight: 700, color: 'var(--text-primary)', marginBottom: '8px' }}>
                  Enroll in Additional Course
                </div>
                
                {eligibleCourses.length > 0 ? (
                  <form onSubmit={handleEnrollCourse} style={{ display: 'flex', gap: '12px', alignItems: 'center', flexWrap: 'wrap' }}>
                    <div style={{ flex: 1, minWidth: '240px' }}>
                      <select
                        className="form-select"
                        value={selectedCourseId}
                        onChange={(e) => setSelectedCourseId(e.target.value)}
                        required
                        disabled={enrolling}
                      >
                        {eligibleCourses.map((ec) => (
                          <option key={ec.id} value={ec.id}>
                            {ec.code} - {ec.title} ({ec.credits} Credits • {ec.courseType})
                          </option>
                        ))}
                      </select>
                    </div>

                    <button
                      type="submit"
                      className="btn btn-primary btn-sm"
                      disabled={enrolling || !selectedCourseId}
                      style={{ display: 'flex', alignItems: 'center', gap: '6px' }}
                    >
                      <Plus size={14} />
                      <span>{enrolling ? 'Enrolling...' : 'Enroll Course'}</span>
                    </button>
                  </form>
                ) : (
                  <div style={{ fontSize: '13px', color: 'var(--text-muted)', display: 'flex', alignItems: 'center', gap: '6px' }}>
                    <CheckCircle size={14} color="var(--success)" />
                    All active eligible courses for this program and semester have been enrolled.
                  </div>
                )}
              </div>

              {/* Footer Buttons */}
              <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '20px' }}>
                <button type="button" className="btn btn-primary" onClick={onClose}>
                  Done
                </button>
              </div>
            </div>
          )}

        </div>
      </div>
    </div>
  );
}
