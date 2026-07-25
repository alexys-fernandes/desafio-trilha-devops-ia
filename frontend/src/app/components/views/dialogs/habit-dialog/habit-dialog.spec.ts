import { CommonModule } from '@angular/common';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { HabitService } from '../../../../services/habit-service';
import { HabitDialog } from './habit-dialog';

describe('HabitDialog', () => {
  let fixture: ComponentFixture<HabitDialog>;

  function view(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [HabitDialog],
      imports: [CommonModule, FormsModule, MatDialogModule],
      providers: [
        {
          provide: MatDialogRef,
          useValue: {
            close: vi.fn(),
          },
        },
        {
          provide: MAT_DIALOG_DATA,
          useValue: null,
        },
        {
          provide: HabitService,
          useValue: {
            buildRecurrenceConfig: vi.fn((type: string, days: string[], interval: number, monthlyDay: number) => {
              if (type === 'SpecificDaysOfWeek') return JSON.stringify({ daysOfWeek: days });
              if (type === 'EveryXDays') return JSON.stringify({ interval });
              if (type === 'Monthly') return JSON.stringify({ dayOfMonth: monthlyDay });
              return null;
            }),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(HabitDialog);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  it('keeps the save button disabled while title and icon are missing', () => {
    const saveButton = view().querySelector(
      '[data-testid="habit-save-button"]',
    ) as HTMLButtonElement;

    expect(saveButton).toBeTruthy();
    expect(saveButton.disabled).toBe(true);
  });

  it('saves specific weekday recurrence config when selected', () => {
    const component = fixture.componentInstance;
    const dialogRef = TestBed.inject(MatDialogRef);

    component.formData.title = 'Read';
    component.formData.icon = '📚';
    component.formData.recurrenceType = 'SpecificDaysOfWeek';
    component.selectedWeekDays = ['Monday', 'Wednesday'];
    component.onSave();

    expect(dialogRef.close).toHaveBeenCalledWith(
      expect.objectContaining({
        recurrenceType: 'SpecificDaysOfWeek',
        recurrenceConfig: JSON.stringify({ daysOfWeek: ['Monday', 'Wednesday'] }),
      }),
    );
  });

  it('saves habit reminder configuration when enabled', () => {
    const component = fixture.componentInstance;
    const dialogRef = TestBed.inject(MatDialogRef);

    component.formData.title = 'Read';
    component.formData.icon = '📚';
    component.formData.reminderEnabled = true;
    component.formData.reminderTime = '08:45';
    component.formData.reminderType = 'Custom';
    component.formData.reminderMessage = 'Read 10 pages before coffee';
    component.onSave();

    expect(dialogRef.close).toHaveBeenCalledWith(
      expect.objectContaining({
        reminderEnabled: true,
        reminderTime: '08:45:00',
        reminderTimezone: 'America/Sao_Paulo',
        reminderType: 'Custom',
        reminderMessage: 'Read 10 pages before coffee',
      }),
    );
  });
});
