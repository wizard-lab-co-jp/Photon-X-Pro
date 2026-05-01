import { writable } from 'svelte/store';

export interface PDFDocument {
  id: string;
  name: string;
  path: string;
  totalPages: number;
  currentPage: number;
  zoom: number;
  pdfImage: string | null;
  thumbnails: string[];
}

function createDocumentStore() {
  const { subscribe, set, update } = writable<{
    documents: PDFDocument[];
    activeId: string | null;
  }>({
    documents: [],
    activeId: null
  });

  return {
    subscribe,
    addDocument: (doc: PDFDocument) => update(state => {
      const exists = state.documents.find(d => d.path === doc.path);
      if (exists) {
        return { ...state, activeId: exists.id };
      }
      return {
        documents: [...state.documents, doc],
        activeId: doc.id
      };
    }),
    removeDocument: (id: string) => update(state => {
      const documents = state.documents.filter(d => d.id !== id);
      let activeId = state.activeId;
      if (activeId === id) {
        activeId = documents.length > 0 ? documents[documents.length - 1].id : null;
      }
      return { documents, activeId };
    }),
    setActive: (id: string) => update(state => ({ ...state, activeId: id })),
    updateDocument: (id: string, updates: Partial<PDFDocument>) => update(state => ({
      ...state,
      documents: state.documents.map(d => d.id === id ? { ...d, ...updates } : d)
    }))
  };
}

export const documentStore = createDocumentStore();
