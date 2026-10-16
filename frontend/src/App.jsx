import React, { useEffect, useState } from 'react';
import { fetchHealth, resetDemoData } from './api';
import Dashboard from './Dashboard';
import StudentDirectory from './StudentDirectory';
import AddStudentForm from './AddStudentForm';
import ProgramsView from './ProgramsView';
import CourseCatalog from './CourseCatalog';
import TermRegistrationsView from './TermRegistrationsView';
import AttendanceView from './AttendanceView';
import FeesView from './FeesView';
import StudentModal from './StudentModal';
import { 
  LayoutDashboard, 
  Users, 
  UserPlus, 
  GraduationCap, 
  BookOpen,
  ClipboardCheck,
  CalendarCheck,
  Receipt,
  RotateCcw, 
  CheckCircle, 
  Building2, 
  Database 
} from 'lucide-react';

export default function App() {
  const [activeTab, setActiveTab] = useState('dashboard');
  const [health, setHealth] = useState({ status: 'CHECKING', isUsingMongoDb: false });
  const [modalStudentId, setModalStudentId] = useState(null);
  const [modalMode, setModalMode] = useState('view');
  const [toast, setToast] = useState(null);
  const [resetting, setResetting] = useState(false);
  const [catalogProgramFilter, setCatalogProgramFilter] = useState('ALL');
  const [directoryProgramFilter, setDirectoryProgramFilter] = useState('ALL');

  useEffect(() => {
    loadHealth();
    const interval = setInterval(loadHealth, 15000);
    return () => clearInterval(interval);
  }, []);

  const loadHealth = async () => {
    const res = await fetchHealth();
    setHealth(res);
  };

  const showToast = (message) => {
    setToast(message);
    setTimeout(() => setToast(null), 4000);
  };

  const handleOpenStudentModal = (id, mode = 'view') => {
    setModalStudentId(id);
    setModalMode(mode);
  };

  const handleCloseStudentModal = () => {
    setModalStudentId(null);
  };

  const handleStudentSaved = (updated) => {
    showToast(`Updated profile for ${updated.fullName} successfully.`);
  };

  const handleResetData = async () => {
    if (!window.confirm('Reset all student records to the default 12 fictional Indian demo students?')) {
      return;
    }
    try {
      setResetting(true);
      await resetDemoData();
      showToast('Demo data reset to initial fictional student records.');
      // Force refresh of views by briefly switching or triggering health
      loadHealth();
    } catch (err) {
      alert('Failed to reset seed data: ' + err.message);
    } finally {
      setResetting(false);
    }
  };

  return (
    <div className="app-container">
      {/* Sidebar Navigation */}
      <aside className="sidebar">
        <div className="sidebar-header">
          <div className="sidebar-logo-icon">VIT</div>
          <div>
            <div className="sidebar-brand-title">Vidyadeep Institute</div>
            <div className="sidebar-brand-subtitle">College Management</div>
          </div>
        </div>

        <nav className="sidebar-nav">
          <button
            className={`nav-item ${activeTab === 'dashboard' ? 'active' : ''}`}
            onClick={() => setActiveTab('dashboard')}
          >
            <LayoutDashboard size={18} />
            <span>Executive Dashboard</span>
          </button>

          <button
            className={`nav-item ${activeTab === 'directory' ? 'active' : ''}`}
            onClick={() => setActiveTab('directory')}
          >
            <Users size={18} />
            <span>Student Directory</span>
          </button>

          <button
            className={`nav-item ${activeTab === 'add-student' ? 'active' : ''}`}
            onClick={() => setActiveTab('add-student')}
          >
            <UserPlus size={18} />
            <span>New Admission</span>
          </button>

          <button
            className={`nav-item ${activeTab === 'programs' ? 'active' : ''}`}
            onClick={() => setActiveTab('programs')}
          >
            <GraduationCap size={18} />
            <span>Degree Programs</span>
          </button>

          <button
            className={`nav-item ${activeTab === 'courses' ? 'active' : ''}`}
            onClick={() => {
              setCatalogProgramFilter('ALL');
              setActiveTab('courses');
            }}
          >
            <BookOpen size={18} />
            <span>Course Catalog</span>
          </button>

          <button
            className={`nav-item ${activeTab === 'registrations' ? 'active' : ''}`}
            onClick={() => setActiveTab('registrations')}
          >
            <ClipboardCheck size={18} />
            <span>Term Registration</span>
          </button>

          <button
            className={`nav-item ${activeTab === 'attendance' ? 'active' : ''}`}
            onClick={() => setActiveTab('attendance')}
          >
            <CalendarCheck size={18} />
            <span>Attendance</span>
          </button>

          <button
            className={`nav-item ${activeTab === 'fees' ? 'active' : ''}`}
            onClick={() => setActiveTab('fees')}
          >
            <Receipt size={18} />
            <span>Fees & Payments</span>
          </button>
        </nav>

        <div className="sidebar-footer">
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '8px' }}>
            <Database size={14} color={health.isUsingMongoDb ? '#34d399' : '#fbbf24'} />
            <span style={{ fontSize: '11px', color: '#e2e8f0', fontWeight: 600 }}>
              {health.isUsingMongoDb ? 'MongoDB Active' : 'In-Memory Fallback'}
            </span>
          </div>
          <button
            className="btn btn-outline btn-sm"
            style={{ width: '100%', fontSize: '11px', padding: '6px 8px', color: '#cbd5e1', borderColor: 'rgba(255,255,255,0.15)' }}
            onClick={handleResetData}
            disabled={resetting}
            title="Reload initial fictional Indian demo student records"
          >
            <RotateCcw size={12} /> {resetting ? 'Resetting...' : 'Reload Demo Data'}
          </button>
          <div style={{ fontSize: '10px', color: '#64748b', marginTop: '10px', textAlign: 'center' }}>
            EduCore MVP v1.0 • Bengaluru
          </div>
        </div>
      </aside>

      {/* Main Content Area */}
      <div className="main-wrapper">
        {/* Topbar Header */}
        <header className="topbar">
          <div className="topbar-left">
            <div className="institution-pill">
              <Building2 size={14} color="var(--accent-600)" />
              <span>Vidyadeep Institute of Technology, Bengaluru</span>
            </div>
          </div>

          <div className="topbar-right">
            <span className={`badge-pill ${health.isUsingMongoDb ? 'online' : 'warning'}`}>
              <span className="badge-dot"></span>
              {health.isUsingMongoDb ? 'Database: MongoDB Server' : 'Storage: Resilient In-Memory'}
            </span>

            <button 
              className="btn btn-primary btn-sm"
              onClick={() => setActiveTab('add-student')}
            >
              <UserPlus size={13} /> Add Student
            </button>
          </div>
        </header>

        {/* Global Toast */}
        {toast && (
          <div style={{ padding: '0 28px', marginTop: '16px' }}>
            <div className="toast-success">
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                <CheckCircle size={16} />
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

        {/* Page Content */}
        <main className="content-container">
          {activeTab === 'dashboard' && (
            <Dashboard 
              onNavigate={(tab) => setActiveTab(tab)}
              onViewStudent={(id) => handleOpenStudentModal(id, 'view')}
            />
          )}

          {activeTab === 'directory' && (
            <StudentDirectory 
              initialProgramFilter={directoryProgramFilter}
              onNavigate={(tab) => setActiveTab(tab)}
              onViewStudent={(id) => handleOpenStudentModal(id, 'view')}
              onEditStudent={(id) => handleOpenStudentModal(id, 'edit')}
            />
          )}

          {activeTab === 'add-student' && (
            <AddStudentForm 
              onNavigate={(tab) => setActiveTab(tab)}
              onViewStudent={(id) => handleOpenStudentModal(id, 'view')}
            />
          )}

          {activeTab === 'programs' && (
            <ProgramsView 
              onSelectProgram={(progCode) => {
                setDirectoryProgramFilter(progCode);
                setActiveTab('directory');
              }}
              onViewProgramCourses={(progCode) => {
                setCatalogProgramFilter(progCode);
                setActiveTab('courses');
              }}
            />
          )}

          {activeTab === 'courses' && (
            <CourseCatalog 
              initialProgramFilter={catalogProgramFilter}
              onNavigate={(tab) => setActiveTab(tab)}
            />
          )}

          {activeTab === 'registrations' && (
            <TermRegistrationsView 
              onNavigate={(tab) => setActiveTab(tab)}
              onViewStudent={(id) => handleOpenStudentModal(id, 'view')}
            />
          )}

          {activeTab === 'attendance' && (
            <AttendanceView 
              onNavigate={(tab) => setActiveTab(tab)}
            />
          )}

          {activeTab === 'fees' && (
            <FeesView 
              onNavigate={(tab) => setActiveTab(tab)}
            />
          )}
        </main>
      </div>

      {/* Student View / Edit Modal */}
      {modalStudentId && (
        <StudentModal
          studentId={modalStudentId}
          initialMode={modalMode}
          onClose={handleCloseStudentModal}
          onSaved={handleStudentSaved}
        />
      )}
    </div>
  );
}
