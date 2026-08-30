import { useEffect, useRef, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import type { ActiveWelcomeCampaign } from '../api/types'
import WelcomeCampaignPlayer from '../components/welcomeCampaign/WelcomeCampaignPlayer'
import '../components/welcomeCampaign/WelcomeCampaignPlayer.css'
import { isWelcomeCampaignSeen, markWelcomeCampaignSeen } from '../utils/welcomeCampaignSeen'

export default function WelcomeCampaignInterstitialPage() {
  const location = useLocation()
  const navigate = useNavigate()
  const destination = (location.state as { destination?: string } | null)?.destination ?? '/'
  const [campaign, setCampaign] = useState<ActiveWelcomeCampaign | null>(null)
  const redirected = useRef(false)

  function goToDestination() {
    if (redirected.current) return
    redirected.current = true
    navigate(destination, { replace: true })
  }

  useEffect(() => {
    let active = true
    api
      .get<ActiveWelcomeCampaign | undefined>('/welcome-campaign/active')
      .then((data) => {
        if (!active) return
        if (!data || data.slides.length === 0) { goToDestination(); return }
        if (isWelcomeCampaignSeen(data.campaignId)) { goToDestination(); return }
        setCampaign(data)
      })
      .catch(() => { if (active) goToDestination() })
    return () => { active = false }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  if (!campaign) return <div className="welcome-campaign-blank" />

  return (
    <WelcomeCampaignPlayer
      slides={campaign.slides}
      onFinished={() => {
        markWelcomeCampaignSeen(campaign.campaignId)
        goToDestination()
      }}
    />
  )
}
