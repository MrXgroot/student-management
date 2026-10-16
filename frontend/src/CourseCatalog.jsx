import React, { useEffect, useState } from 'react';
import { fetchCourses, fetchAcademicPeriods, deleteCourse } from './api';
import CourseModal from './CourseModal';
import { 
  BookOpen, 
  Search, 
  Filter, 
  Plus, 
  Edit3, 
  Trash2, 
  Calendar, 
  Layers, 
  GraduationCap, 
  RefreshCw, 
  AlertCircle,
  CheckCircle2,
  Award
} from 'lucide-react';

const PROGRAMS = [
  'ALL',
  'BCA',
  'B.Sc. Computer Science',
  'B.Com.',
  'BBA',
  'MCA',
  'MBA'
];

export default function CourseCatalog({ initialProgramFilter = 'ALL', onNavigate }) {
  const [courses, setCourses] = useState([]);
  const [periods, setPeriods] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Filters
  const [search, setSearch] = useState('');
  const [programFilter, setProgramFilter] = useState(initialProgramFilter);
  const [semesterFilter, setSemesterFilter] = useState(0);
  const [statusFilter, setStatusFilter] = useState('ALL');

  // Modal State
  const [modalOpen, setModalOpen] = useState(false);
  const [selectedCourse, setSelectedCourse] = useState(null);
  const [isEditMode, setIsEditMode] = useState(false);
  const [toast, setToast] = useState(null);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);
      const [coursesRes, periodsRes] = await Promise.all([
        fetchCourses(search, programFilter, semesterFilter, statusFilter),
        fetchAcademicPeriods().catch(() => [])
      ]);
      setCourses(coursesRes);
      setPeriods(periodsRes);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    const timer = setTimeout(() => {
      loadData();
    }, 200);
    return () => clearTimeout(timer);
  }, [search, programFilter, semesterFilter, statusFilter]);

  const showToast = (msg) => {
    setToast(msg);
    setTimeout(() => setToast(null), 4000);
  };

  const handleOpenAdd = () => {
    setSelectedCourse(null);
    setIsEditMode(false);
    setModalOpen(true);
  };

  const handleOpenEdit = (course) => {
    setSelectedCourse(course);
    setIsEditMode(true);
    setModalOpen(true);
  };

  const handleSaved = (savedCourse) => {
    showToast(`Course ${savedCourse.code} (${savedCourse.title}) saved successfully.`);
    loadData();
  };

  const handleDelete = async (course) => {
    if (!window.confirm(`Are you sure you want to delete course ${course.code} — ${course.title}?`)) {
      return;
    }
    try {
      await deleteCourse(course.id);
      showToast(`Course ${course.code} was removed from the catalog.`);
      loadData();
    } catch (err) {
      alert(`Failed to delete course: ${err.message}`);
    }
  };

  const handleClearFilters = () => {
    setSearch('');
    setProgramFilter('ALL');
    setSemesterFilter(0);
    setStatusFilter('ALL');
  };

  const currentPeriod = periods.find((p) => p.isCurrent) || periods[0];

  // Derived metrics
  const totalCourses = courses.length;
  const coreCourses = courses.filter((c) => c.courseType === 'CORE').length;
  const electiveOrLabCourses = courses.filter((c) => c.courseType !== 'CORE').length;
  const totalCredits = courses.reduce((acc, c) => acc + (c.credits || 0), 0);

  return (
    <div>
      {/* Toast Alert */}
      {toast && (
        <div style={{ marginBottom: '16px' }}>
          <div className="toast-success">
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
              <CheckCircle2 size={16} />
              <span>{toast}</span>
            </div>
            <button 
              onClick={() => setToast(null)}
              style={{ background: 'none', border: 'none', color: 'currentColor', cursor: 'pointer', fontWeight: 700 }}
            >
              ✕
            </button>
          </div>
        </div>
      )}

      {/* Page Header */}
      <div className="page-header">
        <div>
          <h1 className="page-title">Curriculum & Course Catalog</h1>
          <p className="page-subtitle">
            Manage academic syllabus, course credits, semester allocations, and degree requirements
          </p>
        </div>
        <div style={{ display: 'flex', gap: '10px' }}>
          <button className="btn btn-outline" onClick={loadData}>
            <RefreshCw size={14} /> Refresh
          </button>
          <button className="btn btn-primary" onClick={handleOpenAdd}>
            <Plus size={15} /> Add New Course
          </button>
        </div>
      </div>

      {/* Academic Period Banner */}
      {currentPeriod && (
        <div style={{
          backgroundColor: 'var(--surface-50)',
          border: '1px solid var(--surface-200)',
          borderRadius: 'var(--radius-md)',
          padding: '14px 20px',
          marginBottom: '20px',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
          gap: '12px'
        }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
            <div style={{
              width: '38px',
              height: '38px',
              borderRadius: 'var(--radius-sm)',
              backgroundColor: 'rgba(13, 148, 136, 0.1)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: 'var(--accent-600)'
            }}>
              <Calendar size={18} />
            </div>
            <div>
              <span style={{ fontSize: '13.5px', fontWeight: 700, color: 'var(--primary-900)' }}>
                Academic Period: AY {currentPeriod.academicYear} • {currentPeriod.termName}
              </span>
              <p style={{ fontSize: '12px', color: 'var(--text-secondary)', margin: '2px 0 0 0' }}>
                Term Code: <span style={{ fontFamily: 'monospace', fontWeight: 600 }}>{currentPeriod.termCode}</span> • 
                Window: {new Date(currentPeriod.startDate).toLocaleDateString('en-IN', { month: 'short', year: 'numeric' })} to {new Date(currentPeriod.endDate).toLocaleDateString('en-IN', { month: 'short', year: 'numeric' })}
              </p>
            </div>
          </div>
          <span className="badge-pill online">
            <span className="badge-dot"></span> Current Term Active
          </span>
        </div>
      )}

      {/* Metric Cards */}
      <div className="metrics-grid" style={{ marginBottom: '24px' }}>
        <div className="metric-card">
          <div className="metric-icon-box teal">
            <BookOpen size={22} />
          </div>
          <div>
            <div className="metric-value">{totalCourses}</div>
            <div className="metric-label">Total Cataloged Courses</div>
          </div>
        </div>

        <div className="metric-card">
          <div className="metric-icon-box blue">
            <Layers size={22} />
          </div>
          <div>
            <div className="metric-value">{coreCourses}</div>
            <div className="metric-label">Core Theory Subjects</div>
          </div>
        </div>

        <div className="metric-card">
          <div className="metric-icon-box purple">
            <GraduationCap size={22} />
          </div>
          <div>
            <div className="metric-value">{electiveOrLabCourses}</div>
            <div className="metric-label">Electives & Labs</div>
          </div>
        </div>

        <div className="metric-card">
          <div className="metric-icon-box emerald">
            <Award size={22} />
          </div>
          <div>
            <div className="metric-value">{totalCredits}</div>
            <div className="metric-label">Total Curriculum Credits</div>
          </div>
        </div>
      </div>

      {/* Search and Filter Card */}
      <div className="card">
        <div className="card-body" style={{ padding: '16px 20px' }}>
          <div className="filter-bar">
            {/* Search Input */}
            <div className="search-input-wrapper">
              <Search size={16} />
              <input
                type="text"
                className="search-input"
                placeholder="Search by course code, title, or department..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
            </div>

            {/* Filter Dropdowns */}
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px', flexWrap: 'wrap' }}>
              <Filter size={15} color="var(--text-muted)" />
              
              {/* Program Filter */}
              <select
                className="select-filter"
                value={programFilter}
                onChange={(e) => setProgramFilter(e.target.value)}
              >
                {PROGRAMS.map((p) => (
                  <option key={p} value={p}>
                    {p === 'ALL' ? 'All Degree Programs' : p}
                  </option>
                ))}
              </select>

              {/* Semester Filter */}
              <select
                className="select-filter"
                value={semesterFilter}
                onChange={(e) => setSemesterFilter(parseInt(e.target.value, 10))}
              >
                <option value={0}>All Semesters</option>
                <option value={1}>Semester 1 (Odd)</option>
                <option value={2}>Semester 2 (Even)</option>
                <option value={3}>Semester 3 (Odd)</option>
                <option value={4}>Semester 4 (Even)</option>
                <option value={5}>Semester 5 (Odd)</option>
                <option value={6}>Semester 6 (Even)</option>
              </select>

              {/* Status Filter */}
              <select
                className="select-filter"
                value={statusFilter}
                onChange={(e) => setStatusFilter(e.target.value)}
              >
                <option value="ALL">All Statuses</option>
                <option value="ACTIVE">Active Only</option>
                <option value="INACTIVE">Inactive / Archived</option>
              </select>

              {(search || programFilter !== 'ALL' || semesterFilter !== 0 || statusFilter !== 'ALL') && (
                <button className="btn btn-outline btn-sm" onClick={handleClearFilters}>
                  Clear
                </button>
              )}
            </div>
          </div>

          <div style={{ fontSize: '12px', color: 'var(--text-muted)', display: 'flex', justifyContent: 'space-between' }}>
            <span>Showing {courses.length} course offerings</span>
            {(search || programFilter !== 'ALL' || semesterFilter !== 0 || statusFilter !== 'ALL') && (
              <span>Filtered course results</span>
            )}
          </div>
        </div>

        {/* Courses Table */}
        {loading ? (
          <div className="state-box">
            <div className="spinner"></div>
            <p style={{ marginTop: '12px' }}>Loading curriculum course catalog...</p>
          </div>
        ) : error ? (
          <div className="state-box">
            <AlertCircle size={36} color="var(--danger)" className="state-box-icon" />
            <h3 className="state-box-title">Error Loading Courses</h3>
            <p className="state-box-desc">{error}</p>
            <button className="btn btn-primary btn-sm" onClick={loadData}>Retry</button>
          </div>
        ) : courses.length === 0 ? (
          <div className="state-box">
            <BookOpen size={36} className="state-box-icon" />
            <h3 className="state-box-title">No Courses Found</h3>
            <p className="state-box-desc">
              No academic courses match your search or filter criteria.
            </p>
            <div style={{ display: 'flex', gap: '8px' }}>
              <button className="btn btn-outline btn-sm" onClick={handleClearFilters}>
                Clear Filters
              </button>
              <button className="btn btn-primary btn-sm" onClick={handleOpenAdd}>
                <Plus size={13} /> Add Course
              </button>
            </div>
          </div>
        ) : (
          <div className="table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Course Code</th>
                  <th>Title & Description</th>
                  <th>Program & Semester</th>
                  <th>Credits</th>
                  <th>Classification</th>
                  <th>Status</th>
                  <th style={{ textAlign: 'right' }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {courses.map((c) => (
                  <tr key={c.id}>
                    <td>
                      <span style={{
                        fontFamily: 'monospace',
                        fontWeight: 700,
                        fontSize: '13px',
                        backgroundColor: 'var(--surface-100)',
                        padding: '4px 8px',
                        borderRadius: 'var(--radius-sm)',
                        color: 'var(--primary-900)',
                        display: 'inline-block'
                      }}>
                        {c.code}
                      </span>
                    </td>
                    <td>
                      <div style={{ fontWeight: 600, color: 'var(--text-primary)', fontSize: '13.5px' }}>
                        {c.title}
                      </div>
                      {c.description && (
                        <div style={{
                          fontSize: '11.5px',
                          color: 'var(--text-muted)',
                          marginTop: '2px',
                          maxWidth: '420px',
                          whiteSpace: 'nowrap',
                          overflow: 'hidden',
                          textOverflow: 'ellipsis'
                        }}>
                          {c.description}
                        </div>
                      )}
                    </td>
                    <td>
                      <div style={{ fontWeight: 600, fontSize: '13px' }}>{c.programCode}</div>
                      <div style={{ fontSize: '11.5px', color: 'var(--text-muted)' }}>
                        Semester {c.semester} ({c.semester % 2 === 1 ? 'Odd Term' : 'Even Term'})
                      </div>
                    </td>
                    <td>
                      <span style={{
                        fontWeight: 700,
                        color: 'var(--accent-600)',
                        backgroundColor: 'rgba(13, 148, 136, 0.08)',
                        padding: '3px 8px',
                        borderRadius: 'var(--radius-sm)',
                        fontSize: '12px'
                      }}>
                        {c.credits} {c.credits === 1 ? 'Credit' : 'Credits'}
                      </span>
                    </td>
                    <td>
                      <span className="badge-pill" style={{
                        backgroundColor: c.courseType === 'CORE' ? 'var(--surface-100)' : c.courseType === 'LAB' ? '#f0fdf4' : '#faf5ff',
                        color: c.courseType === 'CORE' ? 'var(--text-secondary)' : c.courseType === 'LAB' ? '#059669' : '#9333ea',
                        fontSize: '11px',
                        fontWeight: 600
                      }}>
                        {c.courseType}
                      </span>
                    </td>
                    <td>
                      <span className={`status-tag ${c.status === 'ACTIVE' ? 'active' : 'withdrawn'}`}>
                        {c.status}
                      </span>
                    </td>
                    <td style={{ textAlign: 'right' }}>
                      <div style={{ display: 'inline-flex', gap: '6px' }}>
                        <button
                          className="btn btn-secondary btn-sm"
                          title="Edit Course"
                          onClick={() => handleOpenEdit(c)}
                        >
                          <Edit3 size={13} /> Edit
                        </button>
                        <button
                          className="btn btn-outline btn-sm"
                          title="Delete Course"
                          style={{ color: 'var(--danger)', borderColor: 'rgba(239, 68, 68, 0.3)' }}
                          onClick={() => handleDelete(c)}
                        >
                          <Trash2 size={13} />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Course Create/Edit Modal */}
      {modalOpen && (
        <CourseModal
          course={selectedCourse}
          isEdit={isEditMode}
          onClose={() => setModalOpen(false)}
          onSaved={handleSaved}
        />
      )}
    </div>
  );
}
