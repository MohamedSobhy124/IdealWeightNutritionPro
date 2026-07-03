import {
  Component,
  ElementRef,
  forwardRef,
  Input,
  OnDestroy,
  ViewChild,
  AfterViewInit,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import Quill from 'quill';

@Component({
  selector: 'app-rich-text-editor',
  standalone: true,
  template: `<div #host class="rich-text-host"></div>`,
  styleUrl: './rich-text-editor.component.css',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => RichTextEditorComponent),
      multi: true,
    },
  ],
})
export class RichTextEditorComponent implements ControlValueAccessor, AfterViewInit, OnDestroy {
  @ViewChild('host', { static: true }) host!: ElementRef<HTMLDivElement>;
  @Input() placeholder = '';
  @Input() direction: 'ltr' | 'rtl' = 'ltr';

  private quill?: Quill;
  private pendingValue = '';
  private onChange: (value: string) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  ngAfterViewInit(): void {
    this.quill = new Quill(this.host.nativeElement, {
      theme: 'snow',
      placeholder: this.placeholder,
      modules: {
        toolbar: [
          [{ header: [1, 2, 3, false] }],
          ['bold', 'italic', 'underline', 'strike'],
          [{ list: 'ordered' }, { list: 'bullet' }],
          ['link'],
          ['clean'],
        ],
      },
    });

    this.host.nativeElement.querySelector('.ql-editor')?.setAttribute('dir', this.direction);
    if (this.pendingValue) {
      this.quill.root.innerHTML = this.pendingValue;
    }

    this.quill.on('text-change', () => {
      const html = this.quill?.root.innerHTML ?? '';
      const value = html === '<p><br></p>' ? '' : html;
      this.onChange(value);
    });

    this.quill.on('selection-change', (range) => {
      if (!range) this.onTouched();
    });
  }

  ngOnDestroy(): void {
    this.quill = undefined;
  }

  writeValue(value: string | null): void {
    const html = value ?? '';
    if (this.quill) {
      if (this.quill.root.innerHTML !== html) {
        this.quill.root.innerHTML = html;
      }
    } else {
      this.pendingValue = html;
    }
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.quill?.enable(!isDisabled);
  }
}
