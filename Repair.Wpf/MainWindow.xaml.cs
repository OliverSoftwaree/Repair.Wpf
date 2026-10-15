using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Repair.Core.Datos;
using Repair.Core.Logica;
using Repair.Core.Modelos;
using Repair.Wpf.Integracion;
using Repair.Wpf.Presentacion;

namespace Repair.Wpf
{
    public partial class MainWindow : Window
    {
        private readonly ClienteServicio servicio;
        private readonly IModuloEquipos equipos;
        private readonly bool demostracion;
        private readonly bool? animacionesConfiguradas;
        private bool AnimacionesHabilitadas { get { return animacionesConfiguradas ?? SystemParameters.ClientAreaAnimation; } }
        private readonly ObservableCollection<ClienteFila> filas = new ObservableCollection<ClienteFila>();
        private readonly DispatcherTimer toastTimer;
        private bool ocupado;
        private bool panelAbierto;
        private bool panelEnTransicion;
        private bool datosDisponibles;
        private bool consultaFallida;
        private string busquedaAplicada = "";
        private IInputElement focoAnterior;

        // Constructor para el diseñador: construye la vista sin conectarse a datos.
        public MainWindow()
        {
            InitializeComponent();
            ClientsTable.ItemsSource = filas;
            ClientsCards.ItemsSource = filas;
            toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            toastTimer.Tick += (s, e) => { toastTimer.Stop(); Toast.Visibility = Visibility.Collapsed; };
            Width = Math.Max(MinWidth, Math.Min(1380, SystemParameters.WorkArea.Width * 0.94));
            Height = Math.Max(MinHeight, Math.Min(880, SystemParameters.WorkArea.Height * 0.94));
            ShowPage("Inicio", false);
        }

        public MainWindow(ClienteServicio servicio, bool demostracion, IModuloEquipos equipos, bool? animaciones = null) : this()
        {
            this.servicio = servicio ?? throw new ArgumentNullException(nameof(servicio));
            this.demostracion = demostracion;
            animacionesConfiguradas = animaciones;
            this.equipos = equipos;
            DemoBanner.Visibility = demostracion ? Visibility.Visible : Visibility.Collapsed;
            if (demostracion) { ConnectionText.Text = "Datos de muestra"; ConnectionDot.Fill = Brush("#C58240"); }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyLayout();
            if (servicio != null) await LoadClientsAsync();
        }

        private void ShowPage(string pagina, bool animar = true)
        {
            HomePage.Visibility = pagina == "Inicio" ? Visibility.Visible : Visibility.Collapsed;
            ClientsPage.Visibility = pagina == "Clientes" ? Visibility.Visible : Visibility.Collapsed;
            DevicesPage.Visibility = pagina == "Equipos" ? Visibility.Visible : Visibility.Collapsed;
            NavHome.Tag = pagina == "Inicio" ? "Active" : null;
            NavClients.Tag = pagina == "Clientes" ? "Active" : null;
            NavDevices.Tag = pagina == "Equipos" ? "Active" : null;
            Breadcrumb.Text = pagina == "Inicio" ? "VISTA GENERAL" : pagina.ToUpperInvariant();
            if (animar) AnimatePage();
        }

        private void AnimatePage()
        {
            PageHost.BeginAnimation(OpacityProperty, null); PageHost.Opacity = 1;
            ((TranslateTransform)PageHost.RenderTransform).BeginAnimation(TranslateTransform.YProperty, null);
            ((TranslateTransform)PageHost.RenderTransform).Y = 0;
            if (!AnimacionesHabilitadas) return;
            PageHost.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(230)));
            ((TranslateTransform)PageHost.RenderTransform).BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(12, 0, TimeSpan.FromMilliseconds(260)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        }

        private async Task LoadClientsAsync()
        {
            if (ocupado || servicio == null) return;
            SetBusy(true);
            ResultsText.Text = "Consultando el directorio…";
            ListNotice.Visibility = Visibility.Collapsed;
            string texto = SearchInput.Text.Trim();
            try
            {
                var resultado = await servicio.BuscarAsync(texto);
                filas.Clear();
                foreach (var cliente in resultado) filas.Add(new ClienteFila(cliente));
                busquedaAplicada = texto;
                datosDisponibles = true;
                consultaFallida = false;
                if (texto.Length == 0)
                {
                    HomeCount.Text = resultado.Count.ToString("00");
                    HomeCountHint.Text = demostracion ? "Personas ficticias para explorar Repair." : "Registrados en el directorio del taller.";
                }
                ResultsText.Text = resultado.Count + (resultado.Count == 1 ? " cliente" : " clientes") + (texto.Length > 0 ? " · resultado de búsqueda" : " · directorio completo");
                ConnectionText.Text = demostracion ? "Datos de muestra" : "SQL Server conectado";
                ConnectionDot.Fill = Brush(demostracion ? "#C58240" : "#629582");
            }
            catch (Exception ex)
            {
                filas.Clear(); datosDisponibles = false; consultaFallida = true;
                ListNoticeText.Text = FriendlyMessage(ex, false);
                ListNotice.Visibility = Visibility.Visible;
                ResultsText.Text = "No se pudo consultar el directorio.";
                HomeCount.Text = "—";
                HomeCountHint.Text = "Directorio sin conexión. Reintenta desde Clientes.";
                ConnectionText.Text = "Consulta no disponible";
                ConnectionDot.Fill = Brush("#B96559");
            }
            finally { SetBusy(false); ApplyListState(); }
        }

        private void ApplyListState()
        {
            if (ClientsTable == null) return;
            var disposicion = DisposicionAdaptable.Para(ActualWidth > 0 ? ActualWidth : Width);
            bool hayFilas = datosDisponibles && filas.Count > 0;
            TableContainer.Visibility = hayFilas && !disposicion.ListaCompacta ? Visibility.Visible : Visibility.Collapsed;
            ClientsCards.Visibility = hayFilas && disposicion.ListaCompacta ? Visibility.Visible : Visibility.Collapsed;
            EmptyState.Visibility = hayFilas ? Visibility.Collapsed : Visibility.Visible;
            if (consultaFallida)
            {
                EmptyTitle.Text = "Reconectemos el directorio";
                EmptyDescription.Text = "No pudimos recuperar los clientes. Revisa la conexión del taller y vuelve a intentarlo.";
                EmptyAction.Content = "Reintentar conexión";
            }
            else if (busquedaAplicada.Length > 0)
            {
                EmptyTitle.Text = "Sin coincidencias, por ahora";
                EmptyDescription.Text = "Prueba otro nombre, teléfono, correo o código. También puedes volver al directorio completo.";
                EmptyAction.Content = "Ver todos los clientes";
            }
            else
            {
                EmptyTitle.Text = "Aquí empieza tu directorio";
                EmptyDescription.Text = "Registra al primer cliente para tener sus datos siempre a mano.";
                EmptyAction.Content = "Registrar cliente";
            }
        }

        private void NavHome_Click(object sender, RoutedEventArgs e) { if (!panelAbierto) ShowPage("Inicio"); }
        private void NavClients_Click(object sender, RoutedEventArgs e) { if (!panelAbierto) ShowPage("Clientes"); }
        private void NavDevices_Click(object sender, RoutedEventArgs e)
        {
            if (panelAbierto || ocupado) return;
            if (equipos == null) ShowPage("Equipos");
            else equipos.Abrir(this, servicio);
        }

        private async void Search_Click(object sender, RoutedEventArgs e) { await LoadClientsAsync(); }
        private async void ResetSearch_Click(object sender, RoutedEventArgs e)
        { if (ocupado) return; SearchInput.Clear(); await LoadClientsAsync(); }
        private async void SearchInput_KeyDown(object sender, KeyEventArgs e)
        { if (e.Key == Key.Enter) { e.Handled = true; await LoadClientsAsync(); } }
        private async void EmptyAction_Click(object sender, RoutedEventArgs e)
        {
            if (ocupado) return;
            if (consultaFallida) await LoadClientsAsync();
            else if (busquedaAplicada.Length > 0) { SearchInput.Clear(); await LoadClientsAsync(); }
            else OpenEditor();
        }

        private void NewClient_Click(object sender, RoutedEventArgs e) { OpenEditor(); }
        private void OpenEditor()
        {
            if (ocupado || panelAbierto || servicio == null) return;
            ShowPage("Clientes");
            NameInput.Clear(); PhoneInput.Clear(); EmailInput.Clear(); NotesInput.Clear();
            ClearValidation();
            EditorFields.Visibility = Visibility.Visible;
            DetailFields.Visibility = Visibility.Collapsed;
            SaveButton.Visibility = Visibility.Visible;
            CancelDrawerButton.Content = "Cancelar";
            DrawerKicker.Text = "DIRECTORIO / NUEVO REGISTRO";
            DrawerTitle.Text = "Un nuevo comienzo.";
            OpenDrawer(NameInput);
        }

        private void OpenDrawer(Control foco)
        {
            toastTimer.Stop(); Toast.Visibility = Visibility.Collapsed;
            focoAnterior = Keyboard.FocusedElement;
            panelAbierto = true;
            DrawerOverlay.Visibility = Visibility.Visible;
            Workspace.IsEnabled = false;
            DrawerOverlay.Opacity = 1;
            Dimmer.BeginAnimation(OpacityProperty, null); Dimmer.Opacity = 1;
            DrawerPanel.BeginAnimation(OpacityProperty, null); DrawerPanel.Opacity = 1;
            ((TranslateTransform)DrawerPanel.RenderTransform).BeginAnimation(TranslateTransform.XProperty, null);
            ((TranslateTransform)DrawerPanel.RenderTransform).X = 0;
            if (AnimacionesHabilitadas)
            {
                Dimmer.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)));
                DrawerPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(0.5, 1, TimeSpan.FromMilliseconds(240)));
                ((TranslateTransform)DrawerPanel.RenderTransform).BeginAnimation(TranslateTransform.XProperty,
                    new DoubleAnimation(40, 0, TimeSpan.FromMilliseconds(270)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            }
            Dispatcher.BeginInvoke(new Action(() => { foco.Focus(); Keyboard.Focus(foco); }), DispatcherPriority.Input);
        }

        private async Task CloseDrawerAsync()
        {
            if (!panelAbierto || ocupado || panelEnTransicion) return;
            panelEnTransicion = true;
            if (AnimacionesHabilitadas)
            {
                var terminado = new TaskCompletionSource<bool>();
                var animacion = new DoubleAnimation(0, TimeSpan.FromMilliseconds(160));
                animacion.Completed += (s, e) => terminado.TrySetResult(true);
                Dimmer.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(160)));
                DrawerPanel.BeginAnimation(OpacityProperty, animacion);
                ((TranslateTransform)DrawerPanel.RenderTransform).BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(24, TimeSpan.FromMilliseconds(160)));
                await terminado.Task;
            }
            DrawerOverlay.Visibility = Visibility.Collapsed;
            panelAbierto = false; panelEnTransicion = false;
            Workspace.IsEnabled = true;
            if (focoAnterior != null) Keyboard.Focus(focoAnterior);
        }

        private async void SaveClient_Click(object sender, RoutedEventArgs e)
        {
            if (ocupado || servicio == null || !panelAbierto) return;
            ClearValidation(); SetBusy(true);
            try
            {
                int id = await servicio.RegistrarAsync(new Cliente { Nombre = NameInput.Text, Telefono = PhoneInput.Text, Correo = EmailInput.Text, Notas = NotesInput.Text });
                SetBusy(false);
                await CloseDrawerAsync();
                ShowToast(demostracion ? "Cliente CL-" + id.ToString("0000") + " añadido a la demostración. Es temporal." : "Cliente CL-" + id.ToString("0000") + " registrado en el directorio.");
                SearchInput.Clear();
                await LoadClientsAsync();
            }
            catch (ValidacionException ex)
            {
                foreach (var error in ex.Errores)
                {
                    TextBox campo = error.Key == "Nombre" ? NameInput : error.Key == "Telefono" ? PhoneInput : error.Key == "Correo" ? EmailInput : NotesInput;
                    TextBlock mensaje = error.Key == "Nombre" ? NameError : error.Key == "Telefono" ? PhoneError : error.Key == "Correo" ? EmailError : NotesError;
                    campo.Tag = "Error"; mensaje.Text = error.Value;
                }
                EditorNoticeText.Text = "Revisa los campos señalados. El cliente todavía no se ha guardado.";
                EditorNotice.Visibility = Visibility.Visible;
                SetBusy(false);
                var primero = new[] { NameInput, PhoneInput, EmailInput, NotesInput }.FirstOrDefault(c => Equals(c.Tag, "Error"));
                if (primero != null) primero.Focus();
            }
            catch (Exception ex)
            {
                EditorNoticeText.Text = FriendlyMessage(ex, true);
                EditorNotice.Visibility = Visibility.Visible;
            }
            finally { SetBusy(false); }
        }

        private async void ViewClient_Click(object sender, RoutedEventArgs e)
        { var fila = (sender as FrameworkElement)?.DataContext as ClienteFila; if (fila != null) await OpenDetailAsync(fila.Id); }
        private async void ClientsTable_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var filaVisual = ItemsControl.ContainerFromElement(ClientsTable, e.OriginalSource as DependencyObject) as DataGridRow;
            if (filaVisual?.Item is ClienteFila fila) await OpenDetailAsync(fila.Id);
        }
        private async void ClientsTable_KeyDown(object sender, KeyEventArgs e)
        { if (e.Key == Key.Enter && ClientsTable.SelectedItem is ClienteFila fila) { e.Handled = true; await OpenDetailAsync(fila.Id); } }

        private void ClientsTable_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ClientsTable.Columns.Count != 4 || e.NewSize.Width < 600) return;
            double disponible = e.NewSize.Width - 110;
            ClientsTable.Columns[0].Width = new DataGridLength(disponible * 2 / 4.9);
            ClientsTable.Columns[1].Width = new DataGridLength(disponible * 1.2 / 4.9);
            ClientsTable.Columns[2].Width = new DataGridLength(disponible * 1.7 / 4.9);
        }

        private async Task OpenDetailAsync(int id)
        {
            if (ocupado || panelAbierto || servicio == null) return;
            SetBusy(true);
            try
            {
                var cliente = await servicio.ObtenerAsync(id);
                if (cliente == null) { ShowToast("Ese cliente ya no está disponible. Actualiza el directorio.", false); return; }
                var fila = new ClienteFila(cliente);
                DetailName.Text = cliente.Nombre; DetailInitial.Text = fila.Inicial; DetailCode.Text = fila.Codigo;
                DetailPhone.Text = cliente.Telefono;
                DetailEmail.Text = string.IsNullOrEmpty(cliente.Correo) ? "Sin correo registrado" : cliente.Correo;
                DetailNotes.Text = string.IsNullOrEmpty(cliente.Notas) ? "Sin notas de atención." : cliente.Notas;
                DetailDate.Text = cliente.CreadoUtc.ToLocalTime().ToString("dd/MM/yyyy · HH:mm") + " (hora local)";
                DrawerKicker.Text = "DIRECTORIO / FICHA DEL CLIENTE"; DrawerTitle.Text = "Todo, a mano.";
                EditorFields.Visibility = Visibility.Collapsed; DetailFields.Visibility = Visibility.Visible;
                SaveButton.Visibility = Visibility.Collapsed; CancelDrawerButton.Content = "Cerrar ficha";
                EditorNotice.Visibility = Visibility.Collapsed;
                SetBusy(false); OpenDrawer(CancelDrawerButton);
            }
            catch (Exception ex) { ListNoticeText.Text = FriendlyMessage(ex, false); ListNotice.Visibility = Visibility.Visible; }
            finally { SetBusy(false); }
        }

        private void ClearValidation()
        {
            foreach (var campo in new[] { NameInput, PhoneInput, EmailInput, NotesInput }) campo.Tag = null;
            NameError.Text = ""; PhoneError.Text = ""; EmailError.Text = ""; NotesError.Text = "";
            EditorNotice.Visibility = Visibility.Collapsed;
        }
        private void Input_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!(sender is TextBox campo) || NameError == null) return;
            campo.Tag = null;
            if (campo == NameInput) NameError.Text = "";
            else if (campo == PhoneInput) PhoneError.Text = "";
            else if (campo == EmailInput) EmailError.Text = "";
            else if (campo == NotesInput) NotesError.Text = "";
        }
        private void SetBusy(bool valor)
        {
            ocupado = valor;
            Workspace.IsEnabled = !valor && !panelAbierto;
            DrawerPanel.IsEnabled = !valor;
            BusyIndicator.Visibility = valor ? Visibility.Visible : Visibility.Collapsed;
            BusyIndicator.IsIndeterminate = valor && AnimacionesHabilitadas;
            BusyIndicator.Value = 55;
            SaveButton.Content = valor ? "Guardando…" : "Guardar cliente";
        }
        private static string FriendlyMessage(Exception ex, bool guardando)
        {
            if (ex is AccesoDatosException)
                return guardando ? "No se confirmó el guardado en SQL Server. Conservamos estos datos. Revisa el directorio antes de reintentar para evitar duplicados." : "No pudimos consultar SQL Server. Revisa la conexión y la base del taller, y vuelve a intentarlo.";
            if (ex is ArgumentException) return ex.Message;
            return "La operación no se pudo completar. Revisa el detalle desde Visual Studio antes de continuar.";
        }
        private void ShowToast(string texto, bool exito = true)
        {
            ToastText.Text = texto; Toast.Background = Brush(exito ? "#243D35" : "#314751");
            ToastIcon.Data = (Geometry)FindResource(exito ? "IconCheck" : "IconInfo");
            Toast.Visibility = Visibility.Visible;
            if (AnimacionesHabilitadas) Toast.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));
            toastTimer.Stop(); toastTimer.Start();
        }
        private static SolidColorBrush Brush(string color) { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)); }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) { if (ContentArea != null) ApplyLayout(); }
        private void ApplyLayout()
        {
            var d = DisposicionAdaptable.Para(ActualWidth > 0 ? ActualWidth : Width);
            SidebarColumn.Width = new GridLength(d.MenuCompacto ? 80 : 232);
            SidebarContent.Margin = new Thickness(d.MenuCompacto ? 12 : 18, 28, d.MenuCompacto ? 12 : 18, 20);
            foreach (var item in new FrameworkElement[] { BrandText, NavHomeText, NavClientsText, NavDevicesText, TeamText, NextModuleLabel }) item.Visibility = d.MenuCompacto ? Visibility.Collapsed : Visibility.Visible;
            WorkspaceLabel.Visibility = d.MenuCompacto ? Visibility.Collapsed : Visibility.Visible;
            foreach (var boton in new[] { NavHome, NavClients, NavDevices }) boton.Padding = new Thickness(d.MenuCompacto ? 12 : 14, 12, d.MenuCompacto ? 12 : 14, 12);
            ContentArea.Margin = new Thickness(d.MenuCompacto ? 22 : 34, 24, d.MenuCompacto ? 22 : 34, 22);
            HeroArtColumn.Width = new GridLength(d.MostrarIlustracion ? 270 : 0);
            HeroArt.Visibility = d.MostrarIlustracion ? Visibility.Visible : Visibility.Collapsed;
            HeroTitle.FontSize = d.MenuCompacto ? 34 : 39;
            HomeRightColumn.Width = d.InicioEnColumnas ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
            Grid.SetRow(DirectoryCard, d.InicioEnColumnas ? 0 : 1); Grid.SetColumn(DirectoryCard, d.InicioEnColumnas ? 1 : 0);
            Grid.SetColumnSpan(DirectoryCard, d.InicioEnColumnas ? 1 : 2); Grid.SetColumnSpan(StartCard, d.InicioEnColumnas ? 1 : 2);
            StartCard.Margin = new Thickness(0, 0, d.InicioEnColumnas ? 18 : 0, 0);
            DirectoryCard.Margin = new Thickness(0, d.InicioEnColumnas ? 0 : 16, 0, 0);
            DrawerPanel.Width = d.AnchoPanel;
            ListKeyboardHint.Visibility = d.MenuCompacto ? Visibility.Collapsed : Visibility.Visible;
            ApplyListState();
        }

        private async void CloseDrawer_Click(object sender, RoutedEventArgs e) { await CloseDrawerAsync(); }
        private async void Dimmer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { await CloseDrawerAsync(); }
        private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (ocupado) return;
            if (e.Key == Key.Escape && panelAbierto) { e.Handled = true; await CloseDrawerAsync(); }
            else if (!panelAbierto && Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N) { e.Handled = true; OpenEditor(); }
            else if (!panelAbierto && Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F) { e.Handled = true; ShowPage("Clientes"); SearchInput.Focus(); }
        }
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject origen)
            {
                for (var actual = origen; actual != null; actual = actual is Visual ? VisualTreeHelper.GetParent(actual) : LogicalTreeHelper.GetParent(actual))
                    if (actual is Button) return;
            }
            if (e.ClickCount == 2) ToggleMaximize();
            else if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }
        private void ToggleMaximize() { WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; }
        private void Minimize_Click(object sender, RoutedEventArgs e) { WindowState = WindowState.Minimized; }
        private void Maximize_Click(object sender, RoutedEventArgs e) { ToggleMaximize(); }
        private void Close_Click(object sender, RoutedEventArgs e) { Close(); }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (ocupado || panelEnTransicion) { e.Cancel = true; ShowToast("Espera a que termine la operación antes de cerrar.", false); }
            else toastTimer.Stop();
        }
    }
}
