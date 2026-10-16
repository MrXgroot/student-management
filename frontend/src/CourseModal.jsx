import React, { useState, useEffect } from 'react';
import { createCourse, updateCourse } from './api';
import { X, BookOpen, Save, AlertCircle, CheckCircle, Info } from 'lucide-react';

const PROGRAMS = [
  { code: 'BCA', name: 'Bachelor of Computer Applications', dept: 'Computer Applications', maxSem: 6 },
  { code: 'B.Sc. Computer Science', name: 'Bachelor of Science in Computer Science', dept: 'Computer Science', maxSem: 6 },
  { code: 'B.Com.', name: 'Bachelor of Commerce', dept: 'Commerce & Accountancy', maxSem: 6 },
  { code: 'BBA', name: 'Bachelor of Business Administration', dept: 'Management Studies', maxSem: 6 },
  { code: 'MCA', name: 'Master of Computer Applications', dept: 'Computer Applications', maxSem: 4 },
  { code: 'MBA', name: 'Master of Business Administration', dept: 'Management Studies', maxSem: 4 }
];

export default function CourseModal({ course, isEdit = false, onClose, onSaved }) {
  const [formData, setFormData] = useState({
    code: '',
    title: '',
    credits: 4,
    programCode: 'BCA',
    semester: 1,
    department: 'Computer Applications',
    courseType: 'CORE',
    description: '',
    status: 'ACTIVE'
  });

  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);
  const [fieldErrors, setFieldErrors] = useState({});

  useEffect(() => {
    if (course && isEdit) {
      setFormData({
        code: course.code || '',
        title: course.title || '',
        credits: course.credits || 4,
        programCode: course.programCode || 'BCA',
        semester: course.semester || 1,
        department: course.department || 'Computer Applications',
        courseType: course.courseType || 'CORE',
        description: course.description || '',
        status: course.status || 'ACTIVE'
      });
    }
  }, [course, isEdit]);

  const handleProgramChange = (e) => {
    const selectedProgCode = e.target.value;
    const prog = PROGRAMS.find((p) => p.code === selectedProgCode);
    setFormData((prev) => ({
      ...prev,
      programCode: selectedProgCode,
      department: prog ? prog.dept : prev.department,
      semester: Math.min(prev.semester, prog ? prog.maxSem : 6)
    }));
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: name === 'credits' || name === 'semester' ? parseInt(value, 10) || 1 : value
    }));
    if (fieldErrors[name]) {
      setFieldErrors((prev) => ({ ...prev, [name]: null }));
    }
  };

  const validate = () => {
    const errors = {};
    if (!formData.code || formData.code.trim().length < 3) {
      errors.code = 'Course code is required (min 3 characters, e.g. BCA-101)';
    }
    if (!formData.title || formData.title.trim().length < 3) {
      errors.title = 'Course title is required (min 3 characters)';
    }
    if (formData.credits < 1 || formData.credits > 8) {
      errors.credits = 'Credits must be between 1 and 8';
    }
    if (formData.semester < 1 || formData.semester > 8) {
      errors.semester = 'Semester must be between 1 and 8';
    }
    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validate()) return;

    try {
      setSaving(true);
      setError(null);

      let saved;
      if (isEdit && course?.id) {
        saved = await updateCourse(course.id, formData);
      } else {
        saved = await createCourse(formData);
      }

      onSaved(saved);
      onClose();
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const selectedProg = PROGRAMS.find((p) => p.code === formData.programCode) || PROGRAMS[0];

  return (
    <div className="modal-backdrop">
      <div className="modal-dialog" style={{ maxWidth: '600px' }}>
        <div className="modal-header">
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <div className="sidebar-logo-icon" style={{ width: '32px', height: '32px', fontSize: '13px' }}>
              <BookOpen size={16} />
            </div>
            <div>
              <h2 className="modal-title">
                {isEdit ? `Edit Course: ${course?.code || ''}` : 'Add New Academic Course'}
              </h2>
              <p style={{ fontSize: '12px', color: 'var(--text-muted)', margin: 0 }}>
                {isEdit ? 'Update curriculum syllabus details and credits' : 'Register a new subject in the institutional curriculum'}
              </p>
            </div>
          </div>
          <button className="modal-close" onClick={onClose} aria-label="Close modal">
            <X size={18} />
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="modal-body" style={{ maxHeight: '72vh', overflowY: 'auto', padding: '20px 24px' }}>
            {error && (
              <div style={{
                backgroundColor: '#fef2f2',
                border: '1px solid #fecaca',
                borderRadius: 'var(--radius-sm)',
                padding: '12px 14px',
                marginBottom: '18px',
                display: 'flex',
                alignItems: 'center',
                gap: '10px',
                color: '#b91c1c',
                fontSize: '13px'
              }}>
                <AlertCircle size={18} style={{ flexShrink: 0 }} />
                <span>{error}</span>
              </div>
            )}

            <div className="form-grid">
              {/* Course Code */}
              <div className="form-group">
                <label className="form-label">
                  Course Code <span style={{ color: 'var(--danger)' }}>*</span>
                </label>
                <input
                  type="text"
                  name="code"
                  className={`form-input ${fieldErrors.code ? 'input-error' : ''}`}
                  placeholder="e.g. BCA-201, BSC-102"
                  value={formData.code}
                  onChange={handleChange}
                  style={{ textTransform: 'uppercase', fontFamily: 'monospace', fontWeight: 600 }}
                  required
                />
                {fieldErrors.code && (
                  <span style={{ fontSize: '11.5px', color: 'var(--danger)', marginTop: '4px', display: 'block' }}>
                    {fieldErrors.code}
                  </span>
                )}
              </div>

              {/* Credits */}
              <div className="form-group">
                <label className="form-label">
                  Credits <span style={{ color: 'var(--danger)' }}>*</span>
                </label>
                <select
                  name="credits"
                  className="form-select"
                  value={formData.credits}
                  onChange={handleChange}
                >
                  <option value={1}>1 Credit</option>
                  <option value={2}>2 Credits (Lab / Practical)</option>
                  <option value={3}>3 Credits</option>
                  <option value={4}>4 Credits (Core Theory)</option>
                  <option value={5}>5 Credits</option>
                  <option value={6}>6 Credits (Major Project)</option>
                </select>
              </div>

              {/* Course Title */}
              <div className="form-group full-width">
                <label className="form-label">
                  Course Title <span style={{ color: 'var(--danger)' }}>*</span>
                </label>
                <input
                  type="text"
                  name="title"
                  className={`form-input ${fieldErrors.title ? 'input-error' : ''}`}
                  placeholder="e.g. Database Management Systems"
                  value={formData.title}
                  onChange={handleChange}
                  required
                />
                {fieldErrors.title && (
                  <span style={{ fontSize: '11.5px', color: 'var(--danger)', marginTop: '4px', display: 'block' }}>
                    {fieldErrors.title}
                  </span>
                )}
              </div>

              {/* Associated Program */}
              <div className="form-group">
                <label className="form-label">
                  Degree Program <span style={{ color: 'var(--danger)' }}>*</span>
                </label>
                <select
                  name="programCode"
                  className="form-select"
                  value={formData.programCode}
                  onChange={handleProgramChange}
                >
                  {PROGRAMS.map((p) => (
                    <option key={p.code} value={p.code}>
                      {p.code} — {p.name}
                    </option>
                  ))}
                </select>
              </div>

              {/* Semester */}
              <div className="form-group">
                <label className="form-label">
                  Curriculum Semester <span style={{ color: 'var(--danger)' }}>*</span>
                </label>
                <select
                  name="semester"
                  className="form-select"
                  value={formData.semester}
                  onChange={handleChange}
                >
                  {Array.from({ length: selectedProg.maxSem }, (_, i) => i + 1).map((sem) => (
                    <option key={sem} value={sem}>
                      Semester {sem} ({sem % 2 === 1 ? 'Odd Term' : 'Even Term'})
                    </option>
                  ))}
                </select>
              </div>

              {/* Course Type */}
              <div className="form-group">
                <label className="form-label">Course Classification</label>
                <select
                  name="courseType"
                  className="form-select"
                  value={formData.courseType}
                  onChange={handleChange}
                >
                  <option value="CORE">Core Subject</option>
                  <option value="ELECTIVE">Elective Course</option>
                  <option value="LAB">Practical Laboratory</option>
                </select>
              </div>

              {/* Department */}
              <div className="form-group">
                <label className="form-label">Academic Department</label>
                <input
                  type="text"
                  name="department"
                  className="form-input"
                  value={formData.department}
                  onChange={handleChange}
                />
              </div>

              {/* Status (Edit only) */}
              {isEdit && (
                <div className="form-group">
                  <label className="form-label">Offering Status</label>
                  <select
                    name="status"
                    className="form-select"
                    value={formData.status}
                    onChange={handleChange}
                  >
                    <option value="ACTIVE">ACTIVE (Currently Offered)</option>
                    <option value="INACTIVE">INACTIVE (Discontinued / Archived)</option>
                  </select>
                </div>
              )}

              {/* Description */}
              <div className="form-group full-width">
                <label className="form-label">Course Syllabus / Overview</label>
                <textarea
                  name="description"
                  className="form-textarea"
                  rows={3}
                  placeholder="Summarize course topics, prerequisites, and learning objectives..."
                  value={formData.description}
                  onChange={handleChange}
                />
              </div>
            </div>
          </div>

          <div className="modal-footer" style={{ padding: '16px 24px', backgroundColor: 'var(--surface-50)', borderTop: '1px solid var(--surface-200)', display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
            <button type="button" className="btn btn-outline" onClick={onClose} disabled={saving}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary" disabled={saving}>
              <Save size={14} />
              {saving ? 'Saving...' : isEdit ? 'Update Course' : 'Create Course'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
