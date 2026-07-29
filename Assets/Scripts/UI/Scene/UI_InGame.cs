using UnityEngine;
using UnityEngine.UI;

public class UI_InGame : UI_Scene
{
	enum Texts
	{
		ArtifactText,
		PromptText,
		FuelText,
	}

	enum Images
	{
		FuelFill,
		StaminaFill,
	}

	[SerializeField]
	float _fillSpeed = 5.0f;

	StageProgress _progress;
	PlayerController _player;
	PlayerStatus _status;
	PlayerInteractor _interactor;
	Lamp _lamp;
	bool _ready;

	public void Setup(StageProgress progress, PlayerController player)
	{
		_progress = progress;
		_player = player;

		if (_player != null)
		{
			_status = _player.Status;
			_lamp = _player.Lamp;
			_interactor = _player.GetComponent<PlayerInteractor>();
		}

		RefreshArtifacts();
	}

	public override void Init()
	{
		base.Init();

		Bind<Text>(typeof(Texts));
		Bind<Image>(typeof(Images));

		_ready = true;

		if (_progress == null)
			Setup(FindFirstObjectByType<StageProgress>(), FindFirstObjectByType<PlayerController>());
		else
			RefreshArtifacts();

		if (_progress != null)
			_progress.OnArtifactCollected += OnArtifactCollected;
	}

	void OnDestroy()
	{
		if (_progress != null)
			_progress.OnArtifactCollected -= OnArtifactCollected;
	}

	void OnArtifactCollected(int collected, int required)
	{
		RefreshArtifacts();
	}

	void RefreshArtifacts()
	{
		if (_ready == false || _progress == null)
			return;

		Text text = GetText((int)Texts.ArtifactText);
		if (text != null)
			text.text = $"유물  {_progress.Collected} / {_progress.Required}";
	}

	void Update()
	{
		if (_ready == false)
			return;

		UpdateFill(GetImage((int)Images.StaminaFill), _status == null ? 0 : _status.StaminaRatio);
		UpdateFill(GetImage((int)Images.FuelFill), _lamp == null ? 0 : _lamp.RemainingRatio);

		Text fuelText = GetText((int)Texts.FuelText);
		if (fuelText != null && _lamp != null)
			fuelText.text = _lamp.IsOn ? $"등불  {Mathf.CeilToInt(_lamp.RemainingDuration)}s" : "등불  꺼짐";

		Text prompt = GetText((int)Texts.PromptText);
		if (prompt != null)
		{
			IInteractable target = _interactor == null ? null : _interactor.Current;
			prompt.text = target == null ? string.Empty : target.Prompt;
		}
	}

	void UpdateFill(Image image, float target)
	{
		if (image == null)
			return;

		image.fillAmount = Mathf.MoveTowards(image.fillAmount, target, _fillSpeed * Time.unscaledDeltaTime);
	}
}
