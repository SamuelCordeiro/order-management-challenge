import { DarkMode, LightMode } from '@mui/icons-material';
import { FormControl, IconButton, InputLabel, MenuItem, Select, Stack, Tooltip } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { useColorMode } from './colorMode';
import { supportedLanguages, type SupportedLanguage } from './i18n';

export function AppSettings() {
  const { i18n, t } = useTranslation();
  const { mode, toggleColorMode } = useColorMode();

  return (
    <Stack alignItems="center" direction="row" spacing={1}>
      <FormControl size="small" sx={{ minWidth: 130 }}>
        <InputLabel id="language-selector-label">{t('language')}</InputLabel>
        <Select
          label={t('language')}
          labelId="language-selector-label"
          onChange={(event) => void i18n.changeLanguage(event.target.value as SupportedLanguage)}
          value={i18n.language}
        >
          {supportedLanguages.map((language) => (
            <MenuItem key={language} value={language}>
              {language === 'pt-BR' ? 'Português (Brasil)' : 'English (US)'}
            </MenuItem>
          ))}
        </Select>
      </FormControl>
      <Tooltip title={t(`theme.${mode}`)}>
        <IconButton aria-label={t(`theme.${mode}`)} color="inherit" onClick={toggleColorMode}>
          {mode === 'light' ? <DarkMode /> : <LightMode />}
        </IconButton>
      </Tooltip>
    </Stack>
  );
}
