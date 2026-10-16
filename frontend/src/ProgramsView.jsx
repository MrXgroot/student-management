import React, { useEffect, useState } from 'react';
import { fetchPrograms, fetchProgramDetails } from './api';
import { GraduationCap, BookOpen, Users, ArrowRight, RefreshCw, AlertCircle, X, CheckCircle, Award } from 'lucide-react';

export default function ProgramsView({ onSelectProgram, onViewProgramCourses }) {
  const [programs, setPrograms] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Selected Program Modal
  const [detailModalCode, setDetailModalCode] = useState(null);
  const [detailData, setDetailData] = useState(null);
  const [detailLoading, setDetailLoading] = useState(false);

  const loadPrograms = async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await fetchPrograms();
      setPrograms(res);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadPrograms();
  }, []);

  const handleOpenDetailModal = async (code) => {
    setDetailModalCode(code);
    try {
      setDetailLoading(true);
      const data = await fetchProgramDetails(code);
      setDetailData(data);
    } catch (err) {
      alert(`Failed to load program details: ${err.message}`);
      setDetailModalCode(null);
    } finally {
      setDetailLoading(false);
    }
  };

  const handleCloseDetailModal = () => {
    setDetailModalCode(null);
    setDetailData(null);
  };

  return (
    <div>
      <div className="page-header">
        <div>
          <h1 className="page-title">Academic Degree Programs</h1>
          <p className="page-subtitle">
            Undergraduate and Postgraduate degree specifications offered at Vidyadeep Institute of Technology
          </p>
        </div>
        <div>
          <button className="btn btn-outline" onClick={loadPrograms}>
            <RefreshCw size={14} /> Refresh Programs
          </button>
        </div>
      </div>

      {loading ? (
        <div className="state-box">
          <div className="spinner"></div>
          <p style={{ marginTop: '12px' }}>Loading degree programs...</p>
        </div>
      ) : error ? (
        <div className="card">
          <div className="card-body state-box">
            <AlertCircle size={36} color="var(--danger)" className="state-box-icon" />
            <h3 className="state-box-title">Error Loading Programs</h3>
            <p className="state-box-desc">{error}</p>
            <button className="btn btn-primary btn-sm" onClick={loadPrograms}>Retry</button>
          </div>
        </div>
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(360px, 1fr))', gap: '20px' }}>
          {programs.map((p) => {
            const color = p.color || '#0d9488';
            return (
              <div key={p.code} className="card" style={{ marginBottom: 0, display: 'flex', flexDirection: 'column' }}>
                <div className="card-header" style={{ borderLeft: `4px solid ${color}` }}>
                  <div>
                    <span style={{ fontSize: '12px', fontWeight: 700, color: color, textTransform: 'uppercase', letterSpacing: '0.04em' }}>
                      {p.code}
                    </span>
                    <h3 className="card-title" style={{ fontSize: '15.5px', marginTop: '2px' }}>
                      {p.name}
                    </h3>
                  </div>
                  <span className="badge-pill" style={{ backgroundColor: 'var(--surface-100)', color: 'var(--text-secondary)' }}>
                    {p.award}
                  </span>
                </div>

                <div className="card-body" style={{ display: 'flex', flexDirection: 'column', gap: '12px', fontSize: '13.5px', flexGrow: 1 }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: 'var(--text-secondary)' }}>
                    <BookOpen size={15} color="var(--text-muted)" />
                    <span>Department of {p.department}</span>
                  </div>

                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: 'var(--text-secondary)' }}>
                    <GraduationCap size={15} color="var(--text-muted)" />
                    <span>Duration: {p.duration} ({p.totalSemesters} Semesters)</span>
                  </div>

                  {p.curriculumSummary && (
                    <div style={{
                      backgroundColor: 'var(--surface-50)',
                      padding: '10px 12px',
                      borderRadius: 'var(--radius-sm)',
                      border: '1px solid var(--surface-200)',
                      fontSize: '12px',
                      color: 'var(--text-secondary)'
                    }}>
                      <span style={{ fontWeight: 600, color: 'var(--primary-900)' }}>Core Curriculum: </span>
                      {p.curriculumSummary}
                    </div>
                  )}

                  <div style={{ marginTop: 'auto', paddingTop: '10px', display: 'flex', flexDirection: 'column', gap: '8px' }}>
                    <div style={{ display: 'flex', gap: '8px' }}>
                      <button 
                        className="btn btn-outline btn-sm"
                        style={{ flex: 1, justifyContent: 'center' }}
                        onClick={() => handleOpenDetailModal(p.code)}
                      >
                        <Award size={13} /> Curriculum Details
                      </button>
                      <button 
                        className="btn btn-outline btn-sm"
                        style={{ flex: 1, justifyContent: 'center' }}
                        onClick={() => onViewProgramCourses(p.code)}
                      >
                        <BookOpen size={13} /> Catalog Courses
                      </button>
                    </div>

                    <button 
                      className="btn btn-secondary btn-sm"
                      style={{ width: '100%', justifyContent: 'center' }}
                      onClick={() => onSelectProgram(p.code)}
                    >
                      <Users size={14} /> View Enrolled Students <ArrowRight size={13} />
                    </button>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Program Details Modal */}
      {detailModalCode && (
        <div className="modal-backdrop">
          <div className="modal-dialog" style={{ maxWidth: '680px' }}>
            <div className="modal-header">
              <div>
                <h2 className="modal-title">
                  {detailData?.program?.name || detailModalCode}
                </h2>
                <p style={{ fontSize: '12px', color: 'var(--text-muted)', margin: 0 }}>
                  Curriculum Structure • {detailData?.program?.award} Degree • {detailData?.program?.duration}
                </p>
              </div>
              <button className="modal-close" onClick={handleCloseDetailModal}>
                <X size={18} />
              </button>
            </div>

            <div className="modal-body" style={{ maxHeight: '72vh', overflowY: 'auto', padding: '20px 24px' }}>
              {detailLoading ? (
                <div className="state-box">
                  <div className="spinner"></div>
                  <p style={{ marginTop: '10px' }}>Loading curriculum structure...</p>
                </div>
              ) : detailData ? (
                <div>
                  <div style={{
                    display: 'grid',
                    gridTemplateColumns: 'repeat(3, 1fr)',
                    gap: '12px',
                    marginBottom: '20px'
                  }}>
                    <div style={{ backgroundColor: 'var(--surface-50)', padding: '12px', borderRadius: 'var(--radius-sm)', border: '1px solid var(--surface-200)', textAlign: 'center' }}>
                      <div style={{ fontSize: '18px', fontWeight: 700, color: 'var(--accent-600)' }}>{detailData.courseCount}</div>
                      <div style={{ fontSize: '11px', color: 'var(--text-muted)' }}>Registered Courses</div>
                    </div>
                    <div style={{ backgroundColor: 'var(--surface-50)', padding: '12px', borderRadius: 'var(--radius-sm)', border: '1px solid var(--surface-200)', textAlign: 'center' }}>
                      <div style={{ fontSize: '18px', fontWeight: 700, color: 'var(--primary-800)' }}>{detailData.totalCredits}</div>
                      <div style={{ fontSize: '11px', color: 'var(--text-muted)' }}>Curriculum Credits</div>
                    </div>
                    <div style={{ backgroundColor: 'var(--surface-50)', padding: '12px', borderRadius: 'var(--radius-sm)', border: '1px solid var(--surface-200)', textAlign: 'center' }}>
                      <div style={{ fontSize: '18px', fontWeight: 700, color: 'var(--success)' }}>{detailData.program?.totalSemesters || 6}</div>
                      <div style={{ fontSize: '11px', color: 'var(--text-muted)' }}>Total Semesters</div>
                    </div>
                  </div>

                  <h3 style={{ fontSize: '14px', fontWeight: 700, marginBottom: '12px' }}>
                    Syllabus Courses by Semester
                  </h3>

                  {detailData.courses.length === 0 ? (
                    <p style={{ fontSize: '13px', color: 'var(--text-muted)', textAlign: 'center', padding: '20px' }}>
                      No individual courses assigned to this program yet.
                    </p>
                  ) : (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                      {detailData.courses.map((c) => (
                        <div 
                          key={c.id} 
                          style={{
                            border: '1px solid var(--surface-200)',
                            borderRadius: 'var(--radius-sm)',
                            padding: '12px 14px',
                            display: 'flex',
                            justifyContent: 'space-between',
                            alignItems: 'center',
                            backgroundColor: 'var(--surface-50)'
                          }}
                        >
                          <div>
                            <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                              <span style={{ fontFamily: 'monospace', fontWeight: 700, fontSize: '12px', color: 'var(--primary-900)' }}>
                                {c.code}
                              </span>
                              <span style={{ fontWeight: 600, fontSize: '13px' }}>{c.title}</span>
                              <span className="badge-pill" style={{ fontSize: '10px' }}>{c.courseType}</span>
                            </div>
                            {c.description && (
                              <p style={{ fontSize: '11.5px', color: 'var(--text-muted)', margin: '4px 0 0 0' }}>
                                {c.description}
                              </p>
                            )}
                          </div>
                          <div style={{ textAlign: 'right', flexShrink: 0, marginLeft: '12px' }}>
                            <div style={{ fontSize: '12px', fontWeight: 700, color: 'var(--accent-600)' }}>
                              {c.credits} Credits
                            </div>
                            <div style={{ fontSize: '11px', color: 'var(--text-muted)' }}>
                              Sem {c.semester}
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              ) : null}
            </div>

            <div className="modal-footer" style={{ padding: '14px 24px', backgroundColor: 'var(--surface-50)', borderTop: '1px solid var(--surface-200)', display: 'flex', justifyContent: 'space-between' }}>
              <button 
                type="button" 
                className="btn btn-outline btn-sm"
                onClick={() => {
                  handleCloseDetailModal();
                  onViewProgramCourses(detailModalCode);
                }}
              >
                Open in Course Catalog <ArrowRight size={13} />
              </button>
              <button type="button" className="btn btn-primary btn-sm" onClick={handleCloseDetailModal}>
                Close
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
