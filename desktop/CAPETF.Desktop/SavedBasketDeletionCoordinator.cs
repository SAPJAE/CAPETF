namespace CAPETF.Desktop;

public sealed record SavedBasketDeletionResult(
    bool Deleted,
    IReadOnlyList<SavedSyntheticBasket> SavedBaskets,
    SyntheticBasket? CurrentBasket,
    SyntheticTerminalPayload? ChartPayload);

public sealed class SavedBasketDeletionCoordinator
{
    private readonly SavedSyntheticBasketStore _store;

    public SavedBasketDeletionCoordinator(SavedSyntheticBasketStore store)
    {
        _store = store;
    }

    public bool IsDeleteEnabled(SavedSyntheticBasket? selectedBasket) => selectedBasket is not null;

    public SavedBasketDeletionResult DeleteConfirmed(
        SavedSyntheticBasket selectedBasket,
        SyntheticBasket? currentBasket,
        SyntheticTerminalPayload? chartPayload)
    {
        var deleted = _store.Delete(selectedBasket.Id);
        return new SavedBasketDeletionResult(deleted, _store.LoadAll(), currentBasket, chartPayload);
    }
}
