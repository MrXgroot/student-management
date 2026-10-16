import React, { useState } from 'react';
import { createStudent } from './api';
import { UserPlus, CheckCircle, ArrowLeft, AlertCircle } from 'lucide-react';

const PROGRAMS = [
  'BCA',
  'B.Sc. Computer Science',
  'B.Com.',
  'BBA',
  'MCA',
  'MBA'
];

export default function AddStudentForm({ onNavigate, onViewStudent }) {
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    gender: 'Male',
    dateOfBirth: '2005-04-15',
    program: 'BCA',
    currentSemester: 1,
    address: 'Bengaluru, Karnataka',
    guardianName: '',
    guardianPhone: ''
  });

  const [errors, setErrors] = useState({});
  const [submitting, setSubmitting] = useState(false);
  const [apiError, setApiError] = useState(null);
  const [createdStudent, setCreatedStudent] = useState(null);

  const validate = () => {
    const newErrors = {};
    if (!formData.firstName.trim()) {
      newErrors.firstName = 'First name is required.';
    } else if (formData.firstName.trim().length < 2) {
      newErrors.firstName = 'First name must be at least 2 characters.';
    }

    if (!formData.lastName.trim()) {
      newErrors.lastName = 'Last name is required.';
    }

    if (!formData.email.trim()) {
      newErrors.email = 'Email address is required.';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email.trim())) {
      newErrors.email = 'Please provide a valid email address.';
    }

    if (!formData.phone.trim()) {
      newErrors.phone = 'Phone number is required.';
    } else if (!/^[0-9]{10}$/.test(formData.phone.trim())) {
      newErrors.phone = 'Phone number must be a valid 10-digit mobile number.';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: name === 'currentSemester' ? parseInt(value, 10) : value
    }));
    if (errors[name]) {
      setErrors((prev) => ({ ...prev, [name]: null }));
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validate()) return;

    try {
      setSubmitting(true);
      setApiError(null);
      const payload = {
        ...formData,
        dateOfBirth: new Date(formData.dateOfBirth).toISOString()
      };
      const res = await createStudent(payload);
      setCreatedStudent(res);
    } catch (err) {
      setApiError(err.message);
    } finally {
      setSubmitting(false);
    }
  };

  const handleResetForm = () => {
    setCreatedStudent(null);
    setFormData({
      firstName: '',
      lastName: '',
      email: '',
      phone: '',
      gender: 'Male',
      dateOfBirth: '2005-04-15',
      program: 'BCA',
      currentSemester: 1,
      address: 'Bengaluru, Karnataka',
      guardianName: '',
      guardianPhone: ''
    });
    setErrors({});
  };

  if (createdStudent) {
    return (
      <div style={{ maxWidth: '680px', margin: '40px auto' }}>
        <div className="card">
          <div className="card-body" style={{ padding: '36px', textAlign: 'center' }}>
            <CheckCircle size={56} color="var(--success)" style={{ margin: '0 auto 16px' }} />
            <h2 style={{ fontSize: '22px', fontWeight: 700, color: 'var(--primary-900)' }}>
              Student Registered Successfully!
            </h2>
            <p style={{ color: 'var(--text-secondary)', marginTop: '6px', fontSize: '14px' }}>
              The student profile has been created and assigned an official admission number.
            </p>

            <div style={{
              backgroundColor: 'var(--surface-50)',
              border: '1px dashed var(--accent-600)',
              borderRadius: 'var(--radius-md)',
              padding: '20px',
              margin: '24px 0',
              textAlign: 'left'
            }}>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px', fontSize: '13.5px' }}>
                <div>
                  <span style={{ color: 'var(--text-muted)' }}>Admission Number:</span>
                  <div style={{ fontWeight: 700, fontSize: '16px', color: 'var(--accent-600)', fontFamily: 'monospace' }}>
                    {createdStudent.admissionNumber}
                  </div>
                </div>
                <div>
                  <span style={{ color: 'var(--text-muted)' }}>Student Name:</span>
                  <div style={{ fontWeight: 600 }}>{createdStudent.fullName}</div>
                </div>
                <div>
                  <span style={{ color: 'var(--text-muted)' }}>Program:</span>
                  <div style={{ fontWeight: 600 }}>{createdStudent.program} (Semester {createdStudent.currentSemester})</div>
                </div>
                <div>
                  <span style={{ color: 'var(--text-muted)' }}>Initial Lifecycle Status:</span>
                  <div>
                    <span className="status-tag admitted">{createdStudent.lifecycleStatus}</span>
                  </div>
                </div>
                <div>
                  <span style={{ color: 'var(--text-muted)' }}>Contact Email:</span>
                  <div>{createdStudent.email}</div>
                </div>
                <div>
                  <span style={{ color: 'var(--text-muted)' }}>Contact Phone:</span>
                  <div>{createdStudent.phone}</div>
                </div>
              </div>
            </div>

            <div style={{ display: 'flex', gap: '12px', justifyContent: 'center' }}>
              <button 
                className="btn btn-primary"
                onClick={() => onViewStudent(createdStudent.id)}
              >
                View Full Student Profile
              </button>
              <button 
                className="btn btn-outline"
                onClick={handleResetForm}
              >
                Register Another Student
              </button>
              <button 
                className="btn btn-secondary"
                onClick={() => onNavigate('directory')}
              >
                Back to Directory
              </button>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: '850px', margin: '0 auto' }}>
      <div className="page-header">
        <div>
          <button 
            className="btn btn-outline btn-sm"
            style={{ marginBottom: '8px' }}
            onClick={() => onNavigate('directory')}
          >
            <ArrowLeft size={13} /> Back to Directory
          </button>
          <h1 className="page-title">New Student Admission</h1>
          <p className="page-subtitle">Register a newly admitted student to Vidyadeep Institute of Technology</p>
        </div>
      </div>

      {apiError && (
        <div style={{
          backgroundColor: 'var(--danger-bg)',
          color: 'var(--danger)',
          padding: '14px 18px',
          borderRadius: 'var(--radius-md)',
          marginBottom: '20px',
          display: 'flex',
          alignItems: 'center',
          gap: '10px'
        }}>
          <AlertCircle size={18} />
          <span>{apiError}</span>
        </div>
      )}

      <form onSubmit={handleSubmit}>
        {/* Personal Details */}
        <div className="card">
          <div className="card-header">
            <h2 className="card-title">1. Personal Information</h2>
          </div>
          <div className="card-body">
            <div className="form-grid">
              <div className="form-group">
                <label className="form-label">First Name *</label>
                <input
                  type="text"
                  name="firstName"
                  className="form-input"
                  placeholder="e.g. Ananya"
                  value={formData.firstName}
                  onChange={handleChange}
                />
                {errors.firstName && <span className="form-error">{errors.firstName}</span>}
              </div>

              <div className="form-group">
                <label className="form-label">Last Name *</label>
                <input
                  type="text"
                  name="lastName"
                  className="form-input"
                  placeholder="e.g. Rao"
                  value={formData.lastName}
                  onChange={handleChange}
                />
                {errors.lastName && <span className="form-error">{errors.lastName}</span>}
              </div>

              <div className="form-group">
                <label className="form-label">Gender</label>
                <select
                  name="gender"
                  className="form-select"
                  value={formData.gender}
                  onChange={handleChange}
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
                  value={formData.dateOfBirth}
                  onChange={handleChange}
                />
              </div>
            </div>
          </div>
        </div>

        {/* Academic Details */}
        <div className="card">
          <div className="card-header">
            <h2 className="card-title">2. Academic Program Details</h2>
          </div>
          <div className="card-body">
            <div className="form-grid">
              <div className="form-group">
                <label className="form-label">Degree Program *</label>
                <select
                  name="program"
                  className="form-select"
                  value={formData.program}
                  onChange={handleChange}
                >
                  {PROGRAMS.map((p) => (
                    <option key={p} value={p}>{p}</option>
                  ))}
                </select>
              </div>

              <div className="form-group">
                <label className="form-label">Admitted Semester</label>
                <select
                  name="currentSemester"
                  className="form-select"
                  value={formData.currentSemester}
                  onChange={handleChange}
                >
                  {[1, 2, 3, 4, 5, 6, 7, 8].map((sem) => (
                    <option key={sem} value={sem}>Semester {sem}</option>
                  ))}
                </select>
              </div>
            </div>
          </div>
        </div>

        {/* Contact & Guardian */}
        <div className="card">
          <div className="card-header">
            <h2 className="card-title">3. Contact & Guardian Details</h2>
          </div>
          <div className="card-body">
            <div className="form-grid">
              <div className="form-group">
                <label className="form-label">Student Email *</label>
                <input
                  type="email"
                  name="email"
                  className="form-input"
                  placeholder="e.g. ananya.rao@student.vit.edu"
                  value={formData.email}
                  onChange={handleChange}
                />
                {errors.email && <span className="form-error">{errors.email}</span>}
              </div>

              <div className="form-group">
                <label className="form-label">Student Mobile Number * (10 Digits)</label>
                <input
                  type="tel"
                  name="phone"
                  className="form-input"
                  placeholder="e.g. 9845012345"
                  maxLength={10}
                  value={formData.phone}
                  onChange={handleChange}
                />
                {errors.phone && <span className="form-error">{errors.phone}</span>}
              </div>

              <div className="form-group">
                <label className="form-label">Guardian / Parent Name</label>
                <input
                  type="text"
                  name="guardianName"
                  className="form-input"
                  placeholder="e.g. Suresh Rao"
                  value={formData.guardianName}
                  onChange={handleChange}
                />
              </div>

              <div className="form-group">
                <label className="form-label">Guardian Mobile Number</label>
                <input
                  type="tel"
                  name="guardianPhone"
                  className="form-input"
                  placeholder="e.g. 9845098765"
                  maxLength={10}
                  value={formData.guardianPhone}
                  onChange={handleChange}
                />
              </div>

              <div className="form-group" style={{ gridColumn: '1 / -1' }}>
                <label className="form-label">Residential Address</label>
                <input
                  type="text"
                  name="address"
                  className="form-input"
                  placeholder="e.g. Jayanagar 4th Block, Bengaluru, Karnataka"
                  value={formData.address}
                  onChange={handleChange}
                />
              </div>
            </div>
          </div>
        </div>

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '12px', marginBottom: '40px' }}>
          <button
            type="button"
            className="btn btn-outline"
            onClick={() => onNavigate('directory')}
          >
            Cancel
          </button>
          <button
            type="submit"
            className="btn btn-primary"
            disabled={submitting}
          >
            {submitting ? (
              <>
                <div className="spinner" style={{ width: '14px', height: '14px', borderWidth: '2px' }}></div>
                Registering...
              </>
            ) : (
              <>
                <UserPlus size={15} /> Confirm & Register Student
              </>
            )}
          </button>
        </div>
      </form>
    </div>
  );
}
