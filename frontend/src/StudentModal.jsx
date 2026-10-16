import React, { useEffect, useState } from 'react';
import { fetchStudentById, updateStudent } from './api';
import { X, Edit3, Save, AlertCircle, CheckCircle, User, Mail, Phone, MapPin, Calendar, BookOpen, Shield } from 'lucide-react';

const PROGRAMS = [
  'BCA',
  'B.Sc. Computer Science',
  'B.Com.',
  'BBA',
  'MCA',
  'MBA'
];

const STATUSES = [
  'ADMITTED',
  'ACTIVE',
  'PROBATION',
  'ON_LEAVE',
  'WITHDRAWN',
  'GRADUATED'
];

export default function StudentModal({ studentId, initialMode = 'view', onClose, onSaved }) {
  const [student, setStudent] = useState(null);
  const [loading, setLoading] = useState(true);
  const [mode, setMode] = useState(initialMode);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);
  const [successMsg, setSuccessMsg] = useState(null);

  const [editForm, setEditForm] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    gender: 'Male',
    dateOfBirth: '',
    program: 'BCA',
    currentSemester: 1,
    lifecycleStatus: 'ACTIVE',
    rollNumber: '',
    address: '',
    guardianName: '',
    guardianPhone: ''
  });

  useEffect(() => {
    if (!studentId) return;
    loadStudent();
  }, [studentId]);

  const loadStudent = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await fetchStudentById(studentId);
      setStudent(data);
      setEditForm({
        firstName: data.firstName || '',
        lastName: data.lastName || '',
        email: data.email || '',
        phone: data.phone || '',
        gender: data.gender || 'Male',
        dateOfBirth: data.dateOfBirth ? data.dateOfBirth.split('T')[0] : '',
        program: data.program || 'BCA',
        currentSemester: data.currentSemester || 1,
        lifecycleStatus: data.lifecycleStatus || 'ACTIVE',
        rollNumber: data.rollNumber || '',
        address: data.address || '',
        guardianName: data.guardianName || '',
        guardianPhone: data.guardianPhone || ''
      });
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleEditChange = (e) => {
    const { name, value } = e.target;
    setEditForm((prev) => ({
      ...prev,
      [name]: name === 'currentSemester' ? parseInt(value, 10) : value
    }));
  };

  const handleSave = async (e) => {
    e.preventDefault();
    try {
      setSaving(true);
      setError(null);
      const payload = {
        ...editForm,
        dateOfBirth: editForm.dateOfBirth ? new Date(editForm.dateOfBirth).toISOString() : null
      };
      const updated = await updateStudent(studentId, payload);
      setStudent(updated);
      setSuccessMsg('Student record updated successfully!');
      setTimeout(() => setSuccessMsg(null), 3000);
      setMode('view');
      if (onSaved) onSaved(updated);
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  if (!studentId) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-dialog" onClick={(e) => e.stopPropagation()}>
        {/* Modal Header */}
        <div className="modal-header">
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <h3 className="modal-title">
              {mode === 'edit' ? 'Edit Student Profile' : 'Student Academic Profile'}
            </h3>
            {student && (
              <span className={`status-tag ${student.lifecycleStatus?.toLowerCase()}`}>
                {student.lifecycleStatus}
              </span>
            )}
          </div>
          <button className="modal-close-btn" onClick={onClose}>
            <X size={20} />
          </button>
        </div>

        {/* Modal Body */}
        <div className="modal-body">
          {loading ? (
            <div className="state-box">
              <div className="spinner"></div>
              <p style={{ marginTop: '12px' }}>Loading student information...</p>
            </div>
          ) : error ? (
            <div style={{
              backgroundColor: 'var(--danger-bg)',
              color: 'var(--danger)',
              padding: '14px',
              borderRadius: 'var(--radius-sm)',
              display: 'flex',
              alignItems: 'center',
              gap: '10px',
              marginBottom: '16px'
            }}>
              <AlertCircle size={18} />
              <span>{error}</span>
            </div>
          ) : !student ? (
            <div className="state-box">
              <p>Student record not found.</p>
            </div>
          ) : mode === 'view' ? (
            /* View Mode */
            <div>
              {successMsg && (
                <div style={{
                  backgroundColor: 'var(--success-bg)',
                  color: 'var(--success)',
                  padding: '10px 14px',
                  borderRadius: 'var(--radius-sm)',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '8px',
                  marginBottom: '16px',
                  fontSize: '13.5px'
                }}>
                  <CheckCircle size={16} />
                  <span>{successMsg}</span>
                </div>
              )}

              {/* Top Profile Card */}
              <div style={{
                display: 'flex',
                alignItems: 'center',
                gap: '18px',
                padding: '18px',
                backgroundColor: 'var(--surface-50)',
                border: '1px solid var(--surface-200)',
                borderRadius: 'var(--radius-md)',
                marginBottom: '20px'
              }}>
                <div style={{
                  width: '54px',
                  height: '54px',
                  borderRadius: '50%',
                  background: 'linear-gradient(135deg, var(--accent-500), var(--accent-600))',
                  color: '#fff',
                  fontSize: '20px',
                  fontWeight: 700,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  flexShrink: 0
                }}>
                  {student.firstName?.[0]}{student.lastName?.[0]}
                </div>
                <div>
                  <h2 style={{ fontSize: '18px', fontWeight: 700, color: 'var(--primary-900)' }}>
                    {student.fullName}
                  </h2>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginTop: '4px', fontSize: '13px', color: 'var(--text-secondary)' }}>
                    <span>Admission: <strong style={{ fontFamily: 'monospace', color: 'var(--primary-800)' }}>{student.admissionNumber}</strong></span>
                    <span>•</span>
                    <span>Roll: <strong style={{ fontFamily: 'monospace', color: 'var(--primary-800)' }}>{student.rollNumber || 'Not assigned'}</strong></span>
                  </div>
                </div>
              </div>

              {/* Academic Information */}
              <div style={{ marginBottom: '20px' }}>
                <h4 style={{ fontSize: '13px', textTransform: 'uppercase', color: 'var(--text-muted)', letterSpacing: '0.04em', marginBottom: '10px' }}>
                  Academic Information
                </h4>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: '12px', fontSize: '13.5px' }}>
                  <div style={{ backgroundColor: '#fff', border: '1px solid var(--surface-200)', padding: '12px', borderRadius: 'var(--radius-sm)' }}>
                    <div style={{ color: 'var(--text-muted)', fontSize: '12px' }}>Degree Program</div>
                    <div style={{ fontWeight: 600, color: 'var(--primary-900)' }}>{student.program}</div>
                  </div>
                  <div style={{ backgroundColor: '#fff', border: '1px solid var(--surface-200)', padding: '12px', borderRadius: 'var(--radius-sm)' }}>
                    <div style={{ color: 'var(--text-muted)', fontSize: '12px' }}>Current Semester</div>
                    <div style={{ fontWeight: 600, color: 'var(--primary-900)' }}>Semester {student.currentSemester}</div>
                  </div>
                  <div style={{ backgroundColor: '#fff', border: '1px solid var(--surface-200)', padding: '12px', borderRadius: 'var(--radius-sm)' }}>
                    <div style={{ color: 'var(--text-muted)', fontSize: '12px' }}>Admission Date</div>
                    <div style={{ fontWeight: 600 }}>{new Date(student.admissionDate).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })}</div>
                  </div>
                  <div style={{ backgroundColor: '#fff', border: '1px solid var(--surface-200)', padding: '12px', borderRadius: 'var(--radius-sm)' }}>
                    <div style={{ color: 'var(--text-muted)', fontSize: '12px' }}>Lifecycle Status</div>
                    <div>
                      <span className={`status-tag ${student.lifecycleStatus?.toLowerCase()}`}>
                        {student.lifecycleStatus}
                      </span>
                    </div>
                  </div>
                </div>
              </div>

              {/* Personal & Contact Details */}
              <div style={{ marginBottom: '20px' }}>
                <h4 style={{ fontSize: '13px', textTransform: 'uppercase', color: 'var(--text-muted)', letterSpacing: '0.04em', marginBottom: '10px' }}>
                  Personal & Contact Details
                </h4>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: '12px', fontSize: '13.5px' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <Mail size={15} color="var(--text-muted)" />
                    <span>{student.email}</span>
                  </div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <Phone size={15} color="var(--text-muted)" />
                    <span>{student.phone}</span>
                  </div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <Calendar size={15} color="var(--text-muted)" />
                    <span>DOB: {new Date(student.dateOfBirth).toLocaleDateString('en-IN')} ({student.gender})</span>
                  </div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <MapPin size={15} color="var(--text-muted)" />
                    <span>{student.address || 'Bengaluru, Karnataka'}</span>
                  </div>
                </div>
              </div>

              {/* Guardian Info */}
              <div>
                <h4 style={{ fontSize: '13px', textTransform: 'uppercase', color: 'var(--text-muted)', letterSpacing: '0.04em', marginBottom: '10px' }}>
                  Guardian Details
                </h4>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: '12px', fontSize: '13.5px', backgroundColor: 'var(--surface-50)', padding: '12px', borderRadius: 'var(--radius-sm)' }}>
                  <div>
                    <span style={{ color: 'var(--text-muted)', fontSize: '12px' }}>Guardian Name:</span>
                    <div style={{ fontWeight: 600 }}>{student.guardianName || 'Not specified'}</div>
                  </div>
                  <div>
                    <span style={{ color: 'var(--text-muted)', fontSize: '12px' }}>Guardian Contact:</span>
                    <div style={{ fontWeight: 600 }}>{student.guardianPhone || 'Not specified'}</div>
                  </div>
                </div>
              </div>
            </div>
          ) : (
            /* Edit Mode */
            <form id="edit-student-form" onSubmit={handleSave}>
              <div className="form-grid">
                <div className="form-group">
                  <label className="form-label">First Name *</label>
                  <input
                    type="text"
                    name="firstName"
                    className="form-input"
                    value={editForm.firstName}
                    onChange={handleEditChange}
                    required
                  />
                </div>

                <div className="form-group">
                  <label className="form-label">Last Name *</label>
                  <input
                    type="text"
                    name="lastName"
                    className="form-input"
                    value={editForm.lastName}
                    onChange={handleEditChange}
                    required
                  />
                </div>

                <div className="form-group">
                  <label className="form-label">Email *</label>
                  <input
                    type="email"
                    name="email"
                    className="form-input"
                    value={editForm.email}
                    onChange={handleEditChange}
                    required
                  />
                </div>

                <div className="form-group">
                  <label className="form-label">Phone *</label>
                  <input
                    type="tel"
                    name="phone"
                    className="form-input"
                    value={editForm.phone}
                    onChange={handleEditChange}
                    required
                  />
                </div>

                <div className="form-group">
                  <label className="form-label">Program *</label>
                  <select
                    name="program"
                    className="form-select"
                    value={editForm.program}
                    onChange={handleEditChange}
                  >
                    {PROGRAMS.map((p) => (
                      <option key={p} value={p}>{p}</option>
                    ))}
                  </select>
                </div>

                <div className="form-group">
                  <label className="form-label">Semester</label>
                  <select
                    name="currentSemester"
                    className="form-select"
                    value={editForm.currentSemester}
                    onChange={handleEditChange}
                  >
                    {[1, 2, 3, 4, 5, 6, 7, 8].map((s) => (
                      <option key={s} value={s}>Semester {s}</option>
                    ))}
                  </select>
                </div>

                <div className="form-group">
                  <label className="form-label">Lifecycle Status</label>
                  <select
                    name="lifecycleStatus"
                    className="form-select"
                    value={editForm.lifecycleStatus}
                    onChange={handleEditChange}
                  >
                    {STATUSES.map((s) => (
                      <option key={s} value={s}>{s}</option>
                    ))}
                  </select>
                </div>

                <div className="form-group">
                  <label className="form-label">Class Roll Number</label>
                  <input
                    type="text"
                    name="rollNumber"
                    className="form-input"
                    placeholder="e.g. 24BCA015"
                    value={editForm.rollNumber}
                    onChange={handleEditChange}
                  />
                </div>

                <div className="form-group">
                  <label className="form-label">Gender</label>
                  <select
                    name="gender"
                    className="form-select"
                    value={editForm.gender}
                    onChange={handleEditChange}
                  >
                    <option value="Male">Male</option>
                    <option value="Female">Female</option>
                    <option value="Other">Other</option>
                  </select>
                </div>

                <div className="form-group">
                  <label className="form-label">Date of Birth</label>
                  <input
                    type="date"
                    name="dateOfBirth"
                    className="form-input"
                    value={editForm.dateOfBirth}
                    onChange={handleEditChange}
                  />
                </div>

                <div className="form-group" style={{ gridColumn: '1 / -1' }}>
                  <label className="form-label">Residential Address</label>
                  <input
                    type="text"
                    name="address"
                    className="form-input"
                    value={editForm.address}
                    onChange={handleEditChange}
                  />
                </div>

                <div className="form-group">
                  <label className="form-label">Guardian Name</label>
                  <input
                    type="text"
                    name="guardianName"
                    className="form-input"
                    value={editForm.guardianName}
                    onChange={handleEditChange}
                  />
                </div>

                <div className="form-group">
                  <label className="form-label">Guardian Phone</label>
                  <input
                    type="tel"
                    name="guardianPhone"
                    className="form-input"
                    value={editForm.guardianPhone}
                    onChange={handleEditChange}
                  />
                </div>
              </div>
            </form>
          )}
        </div>

        {/* Modal Footer */}
        <div className="modal-footer">
          {mode === 'view' ? (
            <>
              <button className="btn btn-outline" onClick={onClose}>
                Close
              </button>
              <button className="btn btn-primary" onClick={() => setMode('edit')}>
                <Edit3 size={14} /> Edit Student Profile
              </button>
            </>
          ) : (
            <>
              <button 
                type="button" 
                className="btn btn-outline" 
                onClick={() => setMode('view')}
                disabled={saving}
              >
                Cancel
              </button>
              <button 
                type="submit" 
                form="edit-student-form" 
                className="btn btn-primary"
                disabled={saving}
              >
                {saving ? (
                  <>
                    <div className="spinner" style={{ width: '13px', height: '13px', borderWidth: '2px' }}></div>
                    Saving Changes...
                  </>
                ) : (
                  <>
                    <Save size={14} /> Save Updates
                  </>
                )}
              </button>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
