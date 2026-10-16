import React, { useState, useEffect } from 'react';
import {
  fetchAcademicPeriods,
  fetchCourses,
  fetchAttendanceSheet,
  saveAttendanceBatch,
  fetchAttendanceHistory
} from './api';
import {
  Calendar,
  BookOpen,
  CheckCircle,
  XCircle,
  Clock,
  Save,
  RotateCcw,
  AlertCircle,
  UserCheck,
  History,
  Check,
  X,
  ExternalLink
} from 'lucide-react';

export default function AttendanceView({ onNavigate }) {
  const [periods, setPeriods] = useState([]);
  const [courses, setCourses] = useState([]);
  
  // Selection filters
  const [selectedPeriod, setSelectedPeriod] = useState('2026-ODD');
  const [selectedCourseId, setSelectedCourseId] = useState('');
  const [selectedDate, setSelectedDate] = useState(() => {
    return new Date().toISOString().split('T')[0];
  });

  // Attendance sheet data
  const [sheet, setSheet] = useState(null);
  const [studentMarks, setStudentMarks] = useState({}); // { studentId: { status: 'PRESENT'|'ABSENT', remarks: '' } }
  const [history, setHistory] = useState([]);
  const [showHistory, setShowHistory] = useState(false);

  // Loading & feedback states
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);
  const [successMsg, setSuccessMsg] = useState(null);

  // Load initial periods & courses
  useEffect(() => {
    loadMetadata();
  }, []);

  // When period or course changes, reload sheet and history
  useEffect(() => {
    if (selectedCourseId) {
      loadAttendance();
      loadHistory();
    }
  }, [selectedCourseId, selectedDate, selectedPeriod]);

  const loadMetadata = async () => {
    try {
      setLoading(true);
      const [periodsData, coursesData] = await Promise.all([
        fetchAcademicPeriods(),
        fetchCourses('', 'ALL', 0, 'ACTIVE')
      ]);

      setPeriods(periodsData || []);
      const activePeriod = periodsData?.find(p => p.isCurrent);
      if (activePeriod) {
        setSelectedPeriod(activePeriod.termCode || activePeriod.code || '2026-ODD');
      }

      setCourses(coursesData || []);
      if (coursesData && coursesData.length > 0) {
        // Prefer BCA-101 for immediate demo if present
        const defaultCourse = coursesData.find(c => c.code === 'BCA-101') || coursesData[0];
        setSelectedCourseId(defaultCourse.id);
      }
    } catch (err) {
      setError(err.message || 'Failed to load metadata');
    } finally {
      setLoading(false);
    }
  };

  const loadAttendance = async () => {
    if (!selectedCourseId) return;
    try {
      setLoading(true);
      setError(null);
      setSuccessMsg(null);
      const data = await fetchAttendanceSheet(selectedCourseId, selectedDate, selectedPeriod);
      setSheet(data);

      // Initialize local student marks from sheet
      const marks = {};
      if (data && data.students) {
        data.students.forEach(s => {
          marks[s.studentId] = {
            status: s.status || 'PRESENT', // default to PRESENT for unmarked sessions
            remarks: s.remarks || ''
          };
        });
      }
      setStudentMarks(marks);
    } catch (err) {
      setError(err.message || 'Failed to load attendance sheet');
    } finally {
      setLoading(false);
    }
  };

  const loadHistory = async () => {
    if (!selectedCourseId) return;
    try {
      const historyData = await fetchAttendanceHistory(selectedCourseId, selectedPeriod);
      setHistory(historyData || []);
    } catch (err) {
      console.warn('Failed to load history:', err);
    }
  };

  const handleStatusChange = (studentId, status) => {
    setStudentMarks(prev => ({
      ...prev,
      [studentId]: {
        ...prev[studentId],
        status
      }
    }));
  };

  const handleRemarksChange = (studentId, remarks) => {
    setStudentMarks(prev => ({
      ...prev,
      [studentId]: {
        ...prev[studentId],
        remarks
      }
    }));
  };

  const handleMarkAll = (status) => {
    if (!sheet?.students) return;
    const updated = {};
    sheet.students.forEach(s => {
      updated[s.studentId] = {
        status,
        remarks: studentMarks[s.studentId]?.remarks || ''
      };
    });
    setStudentMarks(updated);
  };

  const handleSave = async () => {
    if (!sheet || !sheet.students || sheet.students.length === 0) return;

    try {
      setSaving(true);
      setError(null);
      setSuccessMsg(null);

      const entries = sheet.students.map(s => ({
        studentId: s.studentId,
        status: studentMarks[s.studentId]?.status || 'PRESENT',
        remarks: studentMarks[s.studentId]?.remarks || null
      }));

      const result = await saveAttendanceBatch({
        academicPeriodCode: selectedPeriod,
        courseId: sheet.courseId,
        classDate: selectedDate,
        entries
      });

      setSheet(result);
      setSuccessMsg(`Attendance saved successfully for ${result.students?.length} students on ${result.classDateString}.`);
      loadHistory();
    } catch (err) {
      setError(err.message || 'Failed to save attendance');
    } finally {
      setSaving(false);
    }
  };

  // Date helper shortcuts
  const setQuickDate = (daysAgo = 0) => {
    const d = new Date();
    d.setDate(d.getDate() - daysAgo);
    setSelectedDate(d.toISOString().split('T')[0]);
  };

  // Calculate live counts
  const totalStudents = sheet?.students?.length || 0;
  const presentCount = Object.values(studentMarks).filter(m => m.status === 'PRESENT').length;
  const absentCount = Object.values(studentMarks).filter(m => m.status === 'ABSENT').length;
  const attendanceRate = totalStudents > 0 ? Math.round((presentCount / totalStudents) * 100) : 0;

  return (
    <div className="view-container">
      {/* Page Header */}
      <div className="page-header" style={{ marginBottom: '20px' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '4px' }}>
            <h1 className="page-title" style={{ margin: 0 }}>Course Attendance</h1>
            <span className="badge badge-info" style={{ fontSize: '11px' }}>
              Term: {selectedPeriod}
            </span>
          </div>
          <p className="page-subtitle" style={{ margin: 0 }}>
            Mark student attendance for course lectures, edit previous records, and track class attendance.
          </p>
        </div>

        <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
          <button
            type="button"
            className={`btn btn-sm ${showHistory ? 'btn-primary' : 'btn-outline'}`}
            onClick={() => setShowHistory(!showHistory)}
            style={{ display: 'inline-flex', alignItems: 'center', gap: '6px' }}
          >
            <History size={14} />
            <span>Past Sessions ({history.length})</span>
          </button>

          <button
            type="button"
            className="btn btn-outline btn-sm"
            onClick={loadAttendance}
            disabled={loading || saving}
            title="Reload attendance data"
          >
            <RotateCcw size={14} className={loading ? 'spin' : ''} />
            <span style={{ marginLeft: '4px' }}>Reload</span>
          </button>
        </div>
      </div>

      {/* Filter Toolbar Card */}
      <div className="card" style={{ padding: '16px 20px', marginBottom: '20px' }}>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '16px', alignItems: 'center' }}>
          
          {/* Period Selector */}
          <div className="form-group" style={{ margin: 0 }}>
            <label className="form-label" style={{ fontSize: '12px', fontWeight: 600, marginBottom: '4px' }}>
              Academic Period
            </label>
            <select
              className="form-select"
              value={selectedPeriod}
              onChange={(e) => setSelectedPeriod(e.target.value)}
              disabled={loading || saving}
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

          {/* Course Selector */}
          <div className="form-group" style={{ margin: 0 }}>
            <label className="form-label" style={{ fontSize: '12px', fontWeight: 600, marginBottom: '4px' }}>
              Course
            </label>
            <select
              className="form-select"
              value={selectedCourseId}
              onChange={(e) => setSelectedCourseId(e.target.value)}
              disabled={loading || saving}
            >
              {courses.map(c => (
                <option key={c.id} value={c.id}>
                  {c.code} — {c.title} ({c.programCode} Sem {c.semester})
                </option>
              ))}
            </select>
          </div>

          {/* Date Picker */}
          <div className="form-group" style={{ margin: 0 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '4px' }}>
              <label className="form-label" style={{ fontSize: '12px', fontWeight: 600, margin: 0 }}>
                Class Date
              </label>
              <div style={{ display: 'flex', gap: '6px' }}>
                <button
                  type="button"
                  onClick={() => setQuickDate(0)}
                  style={{
                    fontSize: '11px',
                    border: 'none',
                    background: selectedDate === new Date().toISOString().split('T')[0] ? 'var(--accent-100)' : 'transparent',
                    color: 'var(--accent-600)',
                    cursor: 'pointer',
                    fontWeight: 600,
                    padding: '1px 6px',
                    borderRadius: '4px'
                  }}
                >
                  Today
                </button>
                <button
                  type="button"
                  onClick={() => setQuickDate(1)}
                  style={{
                    fontSize: '11px',
                    border: 'none',
                    background: 'transparent',
                    color: 'var(--text-muted)',
                    cursor: 'pointer',
                    fontWeight: 600,
                    padding: '1px 4px'
                  }}
                >
                  Yesterday
                </button>
              </div>
            </div>
            <input
              type="date"
              className="form-control"
              value={selectedDate}
              onChange={(e) => setSelectedDate(e.target.value)}
              disabled={loading || saving}
            />
          </div>

        </div>

        {/* Previous Recorded Sessions Drawer */}
        {showHistory && (
          <div style={{
            marginTop: '16px',
            paddingTop: '16px',
            borderTop: '1px solid var(--surface-200)',
            background: 'var(--surface-50)',
            borderRadius: 'var(--radius-sm)',
            padding: '12px 16px'
          }}>
            <div style={{ fontSize: '12px', fontWeight: 700, color: 'var(--text-secondary)', marginBottom: '8px' }}>
              Recorded Attendance Sessions for this Course:
            </div>
            {history.length === 0 ? (
              <div style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
                No attendance sessions recorded yet for this course.
              </div>
            ) : (
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px' }}>
                {history.map(h => (
                  <button
                    key={h.classDateString}
                    type="button"
                    onClick={() => {
                      setSelectedDate(h.classDateString);
                      setShowHistory(false);
                    }}
                    className={`btn btn-sm ${selectedDate === h.classDateString ? 'btn-primary' : 'btn-outline'}`}
                    style={{ fontSize: '12px', padding: '4px 10px' }}
                  >
                    <span>{h.classDateString}</span>
                    <span style={{ opacity: 0.8, marginLeft: '6px' }}>
                      ({h.presentCount}P / {h.absentCount}A)
                    </span>
                  </button>
                ))}
              </div>
            )}
          </div>
        )}
      </div>

      {/* Notifications */}
      {error && (
        <div style={{
          padding: '12px 16px',
          background: '#fef2f2',
          border: '1px solid #fecaca',
          borderRadius: 'var(--radius-md)',
          color: '#b91c1c',
          marginBottom: '20px',
          display: 'flex',
          alignItems: 'center',
          gap: '10px',
          fontSize: '13px'
        }}>
          <AlertCircle size={16} />
          <span>{error}</span>
        </div>
      )}

      {successMsg && (
        <div style={{
          padding: '12px 16px',
          background: '#ecfdf5',
          border: '1px solid #a7f3d0',
          borderRadius: 'var(--radius-md)',
          color: '#065f46',
          marginBottom: '20px',
          display: 'flex',
          alignItems: 'center',
          gap: '10px',
          fontSize: '13px'
        }}>
          <CheckCircle size={16} />
          <span>{successMsg}</span>
        </div>
      )}

      {/* Sheet Summary & Bulk Action Bar */}
      <div style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        flexWrap: 'wrap',
        gap: '12px',
        marginBottom: '16px'
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
          <h2 style={{ fontSize: '16px', fontWeight: 700, margin: 0 }}>
            {sheet?.courseCode} — {sheet?.courseTitle}
          </h2>

          {sheet?.isRecorded ? (
            <span className="badge badge-success" style={{ display: 'inline-flex', alignItems: 'center', gap: '4px' }}>
              <CheckCircle size={12} />
              <span>Recorded (Edit Mode)</span>
            </span>
          ) : (
            <span className="badge badge-info" style={{ display: 'inline-flex', alignItems: 'center', gap: '4px' }}>
              <Clock size={12} />
              <span>New Attendance Sheet</span>
            </span>
          )}
        </div>

        {totalStudents > 0 && (
          <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '13px' }}>
              <span style={{ color: 'var(--text-muted)' }}>Enrolled: <strong>{totalStudents}</strong></span>
              <span>•</span>
              <span style={{ color: 'var(--success)', fontWeight: 600 }}>Present: {presentCount}</span>
              <span>•</span>
              <span style={{ color: 'var(--danger)', fontWeight: 600 }}>Absent: {absentCount}</span>
              <span>•</span>
              <span className={`badge ${attendanceRate >= 75 ? 'badge-success' : 'badge-warning'}`} style={{ fontSize: '11px' }}>
                {attendanceRate}% Rate
              </span>
            </div>

            <div style={{ display: 'flex', gap: '6px' }}>
              <button
                type="button"
                className="btn btn-outline btn-sm"
                onClick={() => handleMarkAll('PRESENT')}
                style={{ fontSize: '11px', padding: '4px 8px', color: 'var(--success)', borderColor: '#a7f3d0' }}
              >
                Mark All Present
              </button>
              <button
                type="button"
                className="btn btn-outline btn-sm"
                onClick={() => handleMarkAll('ABSENT')}
                style={{ fontSize: '11px', padding: '4px 8px', color: 'var(--danger)', borderColor: '#fecaca' }}
              >
                Mark All Absent
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Main Attendance Table Card */}
      <div className="card" style={{ padding: 0, overflow: 'hidden', marginBottom: '24px' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '13px' }}>
          <thead>
            <tr style={{ background: 'var(--surface-50)', borderBottom: '1px solid var(--surface-200)' }}>
              <th style={{ width: '48px', padding: '12px 14px', fontWeight: 600, color: 'var(--text-secondary)' }}>#</th>
              <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)' }}>Student Name & Details</th>
              <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)' }}>Program</th>
              <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)', textAlign: 'center', width: '220px' }}>
                Attendance Status
              </th>
              <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)' }}>Remarks (Optional)</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={5} style={{ padding: '36px', textAlign: 'center', color: 'var(--text-muted)' }}>
                  Loading enrolled students for this course...
                </td>
              </tr>
            ) : totalStudents === 0 ? (
              <tr>
                <td colSpan={5} style={{ padding: '36px', textAlign: 'center' }}>
                  <div style={{ color: 'var(--text-secondary)', fontWeight: 600, marginBottom: '6px' }}>
                    No students currently enrolled in this course for term {selectedPeriod}.
                  </div>
                  <div style={{ color: 'var(--text-muted)', fontSize: '12px', marginBottom: '14px' }}>
                    Students must be registered for the academic term and enrolled in this course to record attendance.
                  </div>
                  <button
                    type="button"
                    className="btn btn-outline btn-sm"
                    onClick={() => onNavigate && onNavigate('registrations')}
                    style={{ display: 'inline-flex', alignItems: 'center', gap: '6px' }}
                  >
                    <span>Go to Term Registration</span>
                    <ExternalLink size={13} />
                  </button>
                </td>
              </tr>
            ) : (
              sheet.students.map((student, idx) => {
                const currentMark = studentMarks[student.studentId] || { status: 'PRESENT', remarks: '' };
                const isPresent = currentMark.status === 'PRESENT';
                const isAbsent = currentMark.status === 'ABSENT';

                return (
                  <tr
                    key={student.studentId}
                    style={{
                      borderBottom: '1px solid var(--surface-200)',
                      backgroundColor: isAbsent ? '#fff5f5' : 'transparent',
                      transition: 'background 0.15s'
                    }}
                  >
                    <td style={{ padding: '12px 14px', color: 'var(--text-muted)', fontWeight: 500 }}>
                      {idx + 1}
                    </td>

                    {/* Student Info */}
                    <td style={{ padding: '12px 16px' }}>
                      <div style={{ fontWeight: 600, color: 'var(--text-primary)', fontSize: '14px' }}>
                        {student.studentName}
                      </div>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginTop: '2px' }}>
                        <span style={{ fontSize: '11px', color: 'var(--text-muted)', fontFamily: 'monospace' }}>
                          {student.admissionNumber}
                        </span>
                        {student.rollNumber && (
                          <span style={{ fontSize: '11px', background: '#e0f2fe', color: '#0369a1', padding: '1px 6px', borderRadius: '4px', fontWeight: 600 }}>
                            Roll: {student.rollNumber}
                          </span>
                        )}
                      </div>
                    </td>

                    {/* Program */}
                    <td style={{ padding: '12px 16px' }}>
                      <span className="badge badge-outline" style={{ fontWeight: 600 }}>
                        {student.programCode} (Sem {student.currentSemester})
                      </span>
                    </td>

                    {/* Status Toggle Buttons */}
                    <td style={{ padding: '12px 16px', textAlign: 'center' }}>
                      <div style={{ display: 'inline-flex', borderRadius: '6px', overflow: 'hidden', border: '1px solid var(--surface-300)' }}>
                        <button
                          type="button"
                          onClick={() => handleStatusChange(student.studentId, 'PRESENT')}
                          style={{
                            display: 'flex',
                            alignItems: 'center',
                            gap: '4px',
                            padding: '6px 14px',
                            fontSize: '12px',
                            fontWeight: 600,
                            border: 'none',
                            cursor: 'pointer',
                            backgroundColor: isPresent ? 'var(--success)' : 'transparent',
                            color: isPresent ? '#fff' : 'var(--text-secondary)',
                            transition: 'all 0.15s'
                          }}
                        >
                          <Check size={13} />
                          <span>Present</span>
                        </button>

                        <button
                          type="button"
                          onClick={() => handleStatusChange(student.studentId, 'ABSENT')}
                          style={{
                            display: 'flex',
                            alignItems: 'center',
                            gap: '4px',
                            padding: '6px 14px',
                            fontSize: '12px',
                            fontWeight: 600,
                            border: 'none',
                            borderLeft: '1px solid var(--surface-300)',
                            cursor: 'pointer',
                            backgroundColor: isAbsent ? 'var(--danger)' : 'transparent',
                            color: isAbsent ? '#fff' : 'var(--text-secondary)',
                            transition: 'all 0.15s'
                          }}
                        >
                          <X size={13} />
                          <span>Absent</span>
                        </button>
                      </div>
                    </td>

                    {/* Remarks Input */}
                    <td style={{ padding: '12px 16px' }}>
                      <input
                        type="text"
                        className="form-control"
                        placeholder="e.g. Late by 10m, Sick leave"
                        value={currentMark.remarks}
                        onChange={(e) => handleRemarksChange(student.studentId, e.target.value)}
                        style={{ fontSize: '12px', padding: '6px 10px' }}
                      />
                    </td>

                  </tr>
                );
              })
            )}
          </tbody>
        </table>

        {/* Footer Save Action Bar */}
        {totalStudents > 0 && (
          <div style={{
            padding: '16px 20px',
            background: 'var(--surface-50)',
            borderTop: '1px solid var(--surface-200)',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            flexWrap: 'wrap',
            gap: '12px'
          }}>
            <div style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
              Marking attendance for <strong>{selectedDate}</strong> • Changes will update existing records without creating duplicates.
            </div>

            <button
              type="button"
              className="btn btn-primary"
              onClick={handleSave}
              disabled={saving || loading}
              style={{ display: 'inline-flex', alignItems: 'center', gap: '6px', minWidth: '160px', justifyContent: 'center' }}
            >
              <Save size={15} />
              <span>{saving ? 'Saving...' : sheet?.isRecorded ? 'Update Attendance' : 'Save Attendance'}</span>
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
