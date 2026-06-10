import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { ChipModule } from 'primeng/chip';
import { IntelligenceService, ChatMessage } from '../../core/services/intelligence.service';
import { ThemeService } from '../../core/services/theme.service';

@Component({
  selector: 'app-ai-assistant-widget',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule, TooltipModule, ChipModule],
  templateUrl: './ai-assistant-widget.component.html',
  styleUrls: ['./ai-assistant-widget.component.scss']
})
export class AiAssistantWidgetComponent {
  private intelligenceService = inject(IntelligenceService);
  private router = inject(Router);
  themeService = inject(ThemeService);

  visible = false;
  loading = false;
  newMessage = '';
  
  messages: ChatMessage[] = [];

  suggestions = [
    '¿Cuántos activos están disponibles?',
    '¿Qué clientes tienen riesgo crítico?',
    'Resumen de deudas morosas',
    '¿Flota en mantenimiento?'
  ];

  toggleChat() {
    this.visible = !this.visible;
    if (this.visible && this.messages.length === 0) {
      this.messages.push({
        role: 'assistant',
        content: '¡Hola! Soy el Asistente IA de MaquiLease. Estoy conectado en tiempo real al estado de los contratos, clientes, pagos e inventario. ¿En qué te puedo ayudar hoy?'
      });
    }
  }

  sendMessage(text?: string) {
    const content = (text || this.newMessage).trim();
    if (!content || this.loading) return;

    // Agregar mensaje de usuario
    this.messages.push({ role: 'user', content });
    this.newMessage = '';
    this.loading = true;

    // Auto-scroll después del renderizado del DOM
    this.scrollChatToBottom();

    // Consultar backend
    this.intelligenceService.chatAssistant({ history: this.messages }).subscribe({
      next: (res) => {
        this.processAssistantResponse(res.response);
        this.loading = false;
        this.scrollChatToBottom();
      },
      error: () => {
        this.messages.push({
          role: 'assistant',
          content: 'Lo siento, ha ocurrido un error al conectar con el servidor de IA de OpenCode. Por favor, inténtelo de nuevo más tarde.'
        });
        this.loading = false;
        this.scrollChatToBottom();
      }
    });
  }

  useSuggestion(suggestion: string) {
    this.sendMessage(suggestion);
  }

  clearChat() {
    this.messages = [
      {
        role: 'assistant',
        content: 'Chat reiniciado. ¿En qué te puedo ayudar hoy?'
      }
    ];
  }

  processAssistantResponse(response: string) {
    let cleanResponse = response;
    const redirectRegex = /\[REDIRECT:([^\]]+)\]/;
    const match = response.match(redirectRegex);
    
    if (match) {
      const route = match[1].trim();
      // Remover el tag de redirección para que no se muestre al usuario
      cleanResponse = response.replace(redirectRegex, '').trim();
      
      this.messages.push({ role: 'assistant', content: cleanResponse });
      
      // Realizar la navegación automática con un ligero delay para que el usuario pueda leer el mensaje
      setTimeout(() => {
        if (this.visible) {
          this.router.navigateByUrl(route);
          this.visible = false; // Cerrar el chat tras la navegación
        }
      }, 1500);
    } else {
      this.messages.push({ role: 'assistant', content: response });
    }
  }

  formatMarkdown(text: string): string {
    if (!text) return '';
    let html = text;
    
    // Remover cualquier tag de redirección si queda alguno en el texto a renderizar
    html = html.replace(/\[REDIRECT:[^\]]+\]/g, '');

    // Escapar caracteres básicos de HTML para evitar inyecciones XSS, manteniendo saltos
    html = html
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;');

    // Convertir enlaces Markdown: [texto](/ruta) -> <a href="/ruta">texto</a>
    // Nota: El estilo usa colores del tema para mayor estética
    html = html.replace(/\[([^\]]+)\]\(([^)]+)\)/g, '<a href="$2" class="chat-redirect-link" style="color: #8b5cf6; font-weight: bold; text-decoration: underline; cursor: pointer;">$1</a>');
    
    // Convertir negritas: **texto** -> <strong>texto</strong>
    html = html.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');
    
    // Convertir viñetas: * elemento o - elemento -> div con viñeta
    html = html.replace(/^\s*[\*\-]\s+(.+)$/gm, '<div style="margin-left: 8px; margin-top: 4px; display: flex; align-items: flex-start; gap: 6px;"><span>•</span><span>$1</span></div>');
    
    return html;
  }

  handleChatBodyClick(event: MouseEvent) {
    const target = event.target as HTMLElement;
    
    // Verificar si se hizo clic en un enlace o en un hijo del enlace
    const anchor = target.closest('a');
    if (anchor) {
      const href = anchor.getAttribute('href');
      if (href && href.startsWith('/')) {
        event.preventDefault();
        this.router.navigateByUrl(href);
        this.visible = false; // Opcional: cerrar el chat tras la navegación para despejar la vista
      }
    }
  }

  private scrollChatToBottom() {
    setTimeout(() => {
      const container = document.getElementById('chat-body-scroll');
      if (container) {
        container.scrollTop = container.scrollHeight;
      }
    }, 100);
  }
}
